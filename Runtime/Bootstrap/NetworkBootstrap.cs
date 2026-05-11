using System;
using Cysharp.Threading.Tasks;
using Emrgry.Core;
using Emrgry.Network.Connection;
using Emrgry.Network.Scene;
using Emrgry.Network.Spawn;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Emrgry.Network.Bootstrap
{
    /// <summary>
    /// Drop-in bootstrap that wires the core network services to <see cref="ServiceLocator"/>.
    /// Place on a GameObject in your Bootstrap scene next to a <see cref="NetworkManager"/>
    /// + <see cref="UnityTransport"/> pair (typically as children). The component is
    /// <c>DontDestroyOnLoad</c> and singleton-guarded.
    ///
    /// <para><b>Two-phase init</b> — required so MonoBehaviours that resolve services in
    /// their <c>Start</c> never race the UGS sign-in (which can take 1–3s on a cold
    /// boot):</para>
    /// <list type="number">
    /// <item><term>Phase 1 (sync, during Awake)</term> <description>EventBus,
    /// NetworkSceneService, PlayerSpawnService. No awaits.</description></item>
    /// <item><term>Phase 2 (async, after Awake returns)</term> <description>UGS
    /// initialization, anonymous sign-in, RelayAdapter, ConnectionService.</description></item>
    /// </list>
    ///
    /// <para><b>Lobby integration</b> — this component intentionally does NOT touch
    /// <c>ILobbyService</c>. If you depend on <c>com.emrgry.lobby</c>, place a
    /// <c>LobbyBootstrap</c> on the same GameObject; it subscribes to
    /// <see cref="UGSReady"/> and registers <c>ILobbyService</c> on its own.</para>
    ///
    /// <para><b>SessionManager</b> — the package's <see cref="Emrgry.Network.Session.SessionManager"/>
    /// is a NetworkBehaviour. Add it to a spawnable prefab in your scene and it will
    /// self-register as <c>ISessionService</c> on <c>OnNetworkSpawn</c>. NetworkBootstrap
    /// does not instantiate it.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkBootstrap : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private NetworkConfigData _config;

        [Header("References (auto-resolved from children if null)")]
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private UnityTransport _transport;

        private IEventBus _eventBus;
        private ConnectionService _connectionService;
        private NetworkSceneService _networkSceneService;
        private PlayerSpawnService _playerSpawnService;
        private RelayAdapter _relayAdapter;
        private bool _initialized;

        public NetworkConfigData Config => _config;
        public IEventBus EventBus => _eventBus;
        public bool IsInitialized => _initialized;

        /// <summary>Fires after Phase-1 services are registered (sync, end of Awake).</summary>
        public event Action CoreServicesReady;
        /// <summary>Fires after UGS init + sign-in completes and Phase-2 services are registered.</summary>
        public event Action UGSReady;

        private async void Awake()
        {
            if (FindObjectsByType<NetworkBootstrap>(FindObjectsSortMode.None).Length > 1)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            await InitializeAsync();
        }

        private async UniTask InitializeAsync()
        {
            // ----- Phase 1: synchronous, runs to completion before any await -----
            if (!ServiceLocator.TryResolve<IEventBus>(out _eventBus))
            {
                _eventBus = new EventBus();
                ServiceLocator.Register<IEventBus>(_eventBus);
            }

            if (_networkManager == null) _networkManager = GetComponentInChildren<NetworkManager>();
            if (_transport == null)      _transport      = GetComponentInChildren<UnityTransport>();

            if (_networkManager == null)
                Debug.LogError("[NetworkBootstrap] No NetworkManager found in children. Assign one in the inspector or add it as a child.");
            if (_transport == null)
                Debug.LogError("[NetworkBootstrap] No UnityTransport found in children. Assign one in the inspector or add it as a child.");

            _networkSceneService = new NetworkSceneService(_networkManager, _eventBus);
            ServiceLocator.Register<INetworkSceneService>(_networkSceneService);

            _playerSpawnService = new PlayerSpawnService(_networkManager);
            ServiceLocator.Register<IPlayerSpawnService>(_playerSpawnService);

            if (_config != null && _networkManager != null)
                _networkManager.NetworkConfig.TickRate = (uint)_config.TickRate;

            CoreServicesReady?.Invoke();

            // ----- Phase 2: UGS-dependent services, AFTER any awaits -----
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[NetworkBootstrap] Signed in anonymously. Player ID: {AuthenticationService.Instance.PlayerId}");
            }

            _relayAdapter = new RelayAdapter();
            ServiceLocator.Register<IRelayAdapter>(_relayAdapter);

            var settings = _config != null
                ? new ConnectionSettings(
                    _config.ConnectTimeoutMs,
                    _config.DisconnectTimeoutMs,
                    _config.HeartbeatTimeoutMs,
                    _config.MaxConnectAttempts,
                    _config.MaxPayloadSize)
                : default;

            _connectionService = new ConnectionService(_networkManager, _transport, _relayAdapter, _eventBus, settings);
            ServiceLocator.Register<IConnectionService>(_connectionService);

            _initialized = true;
            UGSReady?.Invoke();
        }

        private void OnDestroy()
        {
            if (!_initialized) return;

            _connectionService?.Dispose();
            _networkSceneService?.Dispose();

            if (ServiceLocator.TryResolve<IConnectionService>(out _))   ServiceLocator.Deregister<IConnectionService>();
            if (ServiceLocator.TryResolve<INetworkSceneService>(out _)) ServiceLocator.Deregister<INetworkSceneService>();
            if (ServiceLocator.TryResolve<IPlayerSpawnService>(out _))  ServiceLocator.Deregister<IPlayerSpawnService>();
            if (ServiceLocator.TryResolve<IRelayAdapter>(out _))        ServiceLocator.Deregister<IRelayAdapter>();
        }
    }
}
