using System;
using System.Collections.Generic;
using Emrgry.Core;
using Emrgry.Network.Bootstrap;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Emrgry.Network.Session
{
    /// <summary>
    /// Server-authoritative session manager. Tracks player slots (display name,
    /// ready flag, host flag), broadcasts <see cref="SessionPhase"/> changes, and
    /// loads the game scene when the host calls <see cref="StartGame"/>.
    ///
    /// <para>Configuration sources, in priority order:</para>
    /// <list type="number">
    /// <item><description><see cref="NetworkConfigData"/> on the active
    /// <see cref="NetworkBootstrap"/> (preferred).</description></item>
    /// <item><description>The serialized fallback fields on this component
    /// (<c>_maxPlayers</c>, <c>_minPlayersToStart</c>, <c>_gameSceneName</c>).</description></item>
    /// </list>
    /// </summary>
    public sealed class SessionManager : NetworkBehaviour, ISessionService
    {
        [Header("Fallback values when no NetworkConfigData is found")]
        [SerializeField] private int _maxPlayers = 4;
        [SerializeField] private int _minPlayersToStart = 2;
        [SerializeField] private string _gameSceneName = "GameScene";

        private NetworkList<NetworkPlayerSlot> _playerSlots;

        private readonly NetworkVariable<SessionPhase> _phase = new(
            SessionPhase.WaitingForPlayers,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private readonly List<PlayerSlotData> _cachedSlots = new();
        private IEventBus _eventBus;
        private NetworkConfigData _config;

        public SessionPhase Phase => _phase.Value;
        public IReadOnlyList<PlayerSlotData> PlayerSlots => _cachedSlots;
        public int MaxPlayers => _config != null ? _config.MaxPlayers : _maxPlayers;
        public int MinPlayersToStart => _config != null ? _config.MinPlayersToStart : _minPlayersToStart;
        public string GameSceneName =>
            _config != null && !string.IsNullOrEmpty(_config.GameSceneName) ? _config.GameSceneName : _gameSceneName;

        public bool IsEveryoneReady
        {
            get
            {
                int occupiedCount = 0;
                for (int i = 0; i < _playerSlots.Count; i++)
                {
                    if (!_playerSlots[i].IsOccupied) continue;
                    occupiedCount++;
                    if (!_playerSlots[i].IsReady) return false;
                }
                return occupiedCount >= MinPlayersToStart;
            }
        }

        public event Action<SessionPhase> PhaseChanged;
        public event Action<PlayerSlotData> PlayerSlotUpdated;
        public event Action<ulong> PlayerKicked;

        private void Awake()
        {
            _playerSlots = new NetworkList<NetworkPlayerSlot>();
        }

        public override void OnNetworkSpawn()
        {
            if (_eventBus == null && ServiceLocator.TryResolve<IEventBus>(out var bus))
                _eventBus = bus;

            // Pick up the bootstrap config if one exists in the scene.
            var bootstrap = FindFirstObjectByType<NetworkBootstrap>();
            if (bootstrap != null) _config = bootstrap.Config;

            _phase.OnValueChanged += OnPhaseChanged;
            _playerSlots.OnListChanged += OnPlayerSlotsChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += OnServerClientConnected;
                NetworkManager.OnClientDisconnectCallback += OnServerClientDisconnected;
                _phase.Value = SessionPhase.Lobby;
                _eventBus?.Subscribe<SceneLoadCompletedEvent>(OnSceneLoadCompleted);

                foreach (var clientId in NetworkManager.ConnectedClientsIds)
                    OnServerClientConnected(clientId);
            }

            if (!ServiceLocator.TryResolve<ISessionService>(out _))
                ServiceLocator.Register<ISessionService>(this);

            RefreshCachedSlots();
        }

        public override void OnNetworkDespawn()
        {
            _phase.OnValueChanged -= OnPhaseChanged;
            _playerSlots.OnListChanged -= OnPlayerSlotsChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback -= OnServerClientConnected;
                NetworkManager.OnClientDisconnectCallback -= OnServerClientDisconnected;
                _eventBus?.Unsubscribe<SceneLoadCompletedEvent>(OnSceneLoadCompleted);
            }

            if (ServiceLocator.TryResolve<ISessionService>(out _))
                ServiceLocator.Deregister<ISessionService>();
        }

        public void SetLocalPlayerReady(bool ready) => SetReadyServerRpc(ready);
        public void SetLocalPlayerName(string name) => SetDisplayNameServerRpc(name);

        [ServerRpc(RequireOwnership = false)]
        private void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            UpdateSlot(clientId, slot => { slot.IsReady = ready; return slot; });
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetDisplayNameServerRpc(string name, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            UpdateSlot(clientId, slot =>
            {
                slot.DisplayName = new ForceNetworkSerializeByMemcpy<FixedString64Bytes>(new FixedString64Bytes(name));
                return slot;
            });
        }

        public void KickPlayer(ulong clientId)
        {
            if (!IsServer) return;
            RemoveSlot(clientId);
            NetworkManager.DisconnectClient(clientId);
            PlayerKicked?.Invoke(clientId);
            _eventBus?.Publish(new PlayerKickedEvent(clientId, false));
        }

        public void StartGame()
        {
            if (!IsServer) return;
            if (!IsEveryoneReady)
            {
                Debug.LogWarning($"[SessionManager] Cannot start — IsEveryoneReady false. " +
                                 $"(MinPlayersToStart={MinPlayersToStart})");
                return;
            }

            _phase.Value = SessionPhase.Loading;

            var sceneName = GameSceneName;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SessionManager] GameSceneName is empty. Configure it on NetworkConfigData or the fallback field.");
                return;
            }

            if (ServiceLocator.TryResolve<INetworkSceneService>(out var sceneService))
                sceneService.LoadScene(sceneName);
        }

        private void OnServerClientConnected(ulong clientId)
        {
            var slot = new NetworkPlayerSlot
            {
                ClientId = clientId,
                DisplayName = new ForceNetworkSerializeByMemcpy<FixedString64Bytes>(
                    new FixedString64Bytes($"Player {_playerSlots.Count + 1}")),
                IsReady = false,
                IsHost = clientId == NetworkManager.LocalClientId,
                IsOccupied = true,
                SlotIndex = _playerSlots.Count
            };
            _playerSlots.Add(slot);
        }

        private void OnServerClientDisconnected(ulong clientId) => RemoveSlot(clientId);

        private void UpdateSlot(ulong clientId, Func<NetworkPlayerSlot, NetworkPlayerSlot> modifier)
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].ClientId == clientId)
                {
                    _playerSlots[i] = modifier(_playerSlots[i]);
                    return;
                }
            }
        }

        private void RemoveSlot(ulong clientId)
        {
            for (int i = 0; i < _playerSlots.Count; i++)
            {
                if (_playerSlots[i].ClientId != clientId) continue;
                _playerSlots.RemoveAt(i);
                for (int j = i; j < _playerSlots.Count; j++)
                {
                    var s = _playerSlots[j];
                    s.SlotIndex = j;
                    _playerSlots[j] = s;
                }
                return;
            }
        }

        private void OnPhaseChanged(SessionPhase previous, SessionPhase current)
        {
            PhaseChanged?.Invoke(current);
            _eventBus?.Publish(new SessionPhaseChangedEvent(current, previous));
        }

        private void OnPlayerSlotsChanged(NetworkListEvent<NetworkPlayerSlot> changeEvent)
        {
            RefreshCachedSlots();
            var data = changeEvent.Index < _cachedSlots.Count
                ? _cachedSlots[changeEvent.Index]
                : new PlayerSlotData();
            PlayerSlotUpdated?.Invoke(data);
            _eventBus?.Publish(new PlayerSlotUpdatedEvent(data));
        }

        private void RefreshCachedSlots()
        {
            _cachedSlots.Clear();
            ulong localId = NetworkManager.LocalClientId;
            for (int i = 0; i < _playerSlots.Count; i++)
                _cachedSlots.Add(_playerSlots[i].ToData(localId));
        }

        private void OnSceneLoadCompleted(SceneLoadCompletedEvent evt)
        {
            if (!IsServer || _phase.Value != SessionPhase.Loading) return;
            _phase.Value = SessionPhase.InGame;
        }
    }
}
