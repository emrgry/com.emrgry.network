using System;
using Cysharp.Threading.Tasks;
using Emrgry.Core;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Emrgry.Network.Connection
{
    public sealed class ConnectionService : IConnectionService, IDisposable
    {
        private readonly NetworkManager _networkManager;
        private readonly UnityTransport _transport;
        private readonly IRelayAdapter _relayAdapter;
        private readonly IEventBus _eventBus;
        private readonly ConnectionSettings _settings;

        private ConnectionState _state = ConnectionState.Disconnected;
        private string _currentJoinCode = string.Empty;

        public bool IsHost => _networkManager.IsHost;
        public bool IsConnected => _state == ConnectionState.Connected;
        public ConnectionRole Role => IsHost ? ConnectionRole.Host : ConnectionRole.Client;
        public string CurrentJoinCode => _currentJoinCode;

        public event Action<DisconnectReason> Disconnected;
        public event Action<ulong> ClientConnected;
        public event Action<ulong> ClientDisconnected;

        public ConnectionService(
            NetworkManager networkManager,
            UnityTransport transport,
            IRelayAdapter relayAdapter,
            IEventBus eventBus,
            ConnectionSettings settings = default)
        {
            _networkManager = networkManager;
            _transport = transport;
            _relayAdapter = relayAdapter;
            _eventBus = eventBus;
            _settings = settings;

            _networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
            _networkManager.OnClientDisconnectCallback += OnClientDisconnectCallback;
        }

        public async UniTask<ConnectionResult> StartHostAsync(HostConfig config)
        {
            if (_state != ConnectionState.Disconnected)
                return ConnectionResult.Failed("Already connected or connecting.");

            _state = ConnectionState.Connecting;

            try
            {
                if (config.UseRelay)
                {
                    var relayResult = await _relayAdapter.CreateAllocationAsync(config.MaxPlayers);
                    if (!relayResult.Success)
                    {
                        _state = ConnectionState.Disconnected;
                        return ConnectionResult.Failed(relayResult.ErrorMessage);
                    }

                    _transport.SetRelayServerData(relayResult.ServerData);
                    _currentJoinCode = relayResult.JoinCode;
                }
                else
                {
                    _transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");
                    _currentJoinCode = "LOCAL";
                }

                ApplyTransportSettings();

                if (!_networkManager.StartHost())
                {
                    _state = ConnectionState.Disconnected;
                    return ConnectionResult.Failed("NetworkManager.StartHost() failed.");
                }

                _state = ConnectionState.Connected;
                _eventBus.Publish(new NetworkConnectedEvent(true, _currentJoinCode));

                return ConnectionResult.Succeeded(_currentJoinCode);
            }
            catch (Exception e)
            {
                _state = ConnectionState.Disconnected;
                return ConnectionResult.Failed(e.Message);
            }
        }

        public async UniTask<ConnectionResult> JoinAsync(JoinConfig config)
        {
            if (_state != ConnectionState.Disconnected)
                return ConnectionResult.Failed("Already connected or connecting.");

            _state = ConnectionState.Connecting;

            try
            {
                if (config.UseRelay)
                {
                    var relayResult = await _relayAdapter.JoinAllocationAsync(config.JoinCode);
                    if (!relayResult.Success)
                    {
                        _state = ConnectionState.Disconnected;
                        return ConnectionResult.Failed(relayResult.ErrorMessage);
                    }

                    _transport.SetRelayServerData(relayResult.ServerData);
                }
                else
                {
                    _transport.SetConnectionData("127.0.0.1", 7777);
                }

                ApplyTransportSettings();

                if (!_networkManager.StartClient())
                {
                    _state = ConnectionState.Disconnected;
                    return ConnectionResult.Failed("NetworkManager.StartClient() failed.");
                }

                _state = ConnectionState.Connected;
                _currentJoinCode = config.JoinCode;
                _eventBus.Publish(new NetworkConnectedEvent(false, config.JoinCode));

                return ConnectionResult.Succeeded(config.JoinCode);
            }
            catch (Exception e)
            {
                _state = ConnectionState.Disconnected;
                return ConnectionResult.Failed(e.Message);
            }
        }

        public UniTask DisconnectAsync()
        {
            if (_state == ConnectionState.Disconnected)
                return UniTask.CompletedTask;

            _state = ConnectionState.Disconnecting;
            _networkManager.Shutdown();
            _state = ConnectionState.Disconnected;
            _currentJoinCode = string.Empty;

            _eventBus.Publish(new NetworkDisconnectedEvent(DisconnectReason.Intentional));
            Disconnected?.Invoke(DisconnectReason.Intentional);

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            if (_networkManager != null)
            {
                _networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
                _networkManager.OnClientDisconnectCallback -= OnClientDisconnectCallback;
            }
        }

        private void ApplyTransportSettings()
        {
            if (_settings.ConnectTimeoutMs > 0)
                _transport.ConnectTimeoutMS = _settings.ConnectTimeoutMs;
            if (_settings.DisconnectTimeoutMs > 0)
                _transport.DisconnectTimeoutMS = _settings.DisconnectTimeoutMs;
            if (_settings.HeartbeatTimeoutMs > 0)
                _transport.HeartbeatTimeoutMS = _settings.HeartbeatTimeoutMs;
            if (_settings.MaxConnectAttempts > 0)
                _transport.MaxConnectAttempts = _settings.MaxConnectAttempts;
            if (_settings.MaxPayloadSize > 0)
                _transport.MaxPayloadSize = _settings.MaxPayloadSize;
        }

        private void OnClientConnectedCallback(ulong clientId)
        {
            Debug.Log($"[ConnectionService] Client connected: {clientId}");
            ClientConnected?.Invoke(clientId);
        }

        private void OnClientDisconnectCallback(ulong clientId)
        {
            Debug.Log($"[ConnectionService] Client disconnected: {clientId}");
            ClientDisconnected?.Invoke(clientId);

            if (clientId == _networkManager.LocalClientId && _state == ConnectionState.Connected)
            {
                var reason = _networkManager.IsHost ? DisconnectReason.Intentional : DisconnectReason.HostLeft;
                _state = ConnectionState.Disconnected;
                _currentJoinCode = string.Empty;
                _eventBus.Publish(new NetworkDisconnectedEvent(reason));
                Disconnected?.Invoke(reason);
            }
        }

        private enum ConnectionState { Disconnected, Connecting, Connected, Disconnecting }
    }

    /// <summary>
    /// Transport timeout/payload settings. All values optional — zero means use Unity defaults.
    /// </summary>
    public readonly struct ConnectionSettings
    {
        public readonly int ConnectTimeoutMs;
        public readonly int DisconnectTimeoutMs;
        public readonly int HeartbeatTimeoutMs;
        public readonly int MaxConnectAttempts;
        public readonly int MaxPayloadSize;

        public ConnectionSettings(
            int connectTimeoutMs = 0,
            int disconnectTimeoutMs = 0,
            int heartbeatTimeoutMs = 0,
            int maxConnectAttempts = 0,
            int maxPayloadSize = 0)
        {
            ConnectTimeoutMs = connectTimeoutMs;
            DisconnectTimeoutMs = disconnectTimeoutMs;
            HeartbeatTimeoutMs = heartbeatTimeoutMs;
            MaxConnectAttempts = maxConnectAttempts;
            MaxPayloadSize = maxPayloadSize;
        }
    }
}
