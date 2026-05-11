using UnityEngine;

namespace Emrgry.Network.Bootstrap
{
    /// <summary>
    /// Project-level network configuration asset. Assign one instance to a
    /// <see cref="NetworkBootstrap"/> via the inspector. All values are read once
    /// at bootstrap time and pushed into NetworkManager / UnityTransport / ConnectionService.
    ///
    /// <para>The package also reads <see cref="GameSceneName"/> and <see cref="LobbySceneName"/>
    /// when present, so a `SessionManager` or `LobbyConnectionFlow` does not have to
    /// hardcode scene names. Leave the scene fields empty to keep the caller fully
    /// responsible for scene naming.</para>
    /// </summary>
    [CreateAssetMenu(fileName = "NetworkConfigData", menuName = "Emrgry/Network/NetworkConfigData")]
    public sealed class NetworkConfigData : ScriptableObject
    {
        [Header("Simulation")]
        [Tooltip("Server simulation tick rate (Hz). Must match across host and clients.")]
        [SerializeField] private int _tickRate = 30;

        [Header("Connection")]
        [Tooltip("Maximum number of connected players including the host.")]
        [SerializeField] private int _maxPlayers = 4;

        [Tooltip("Minimum players required for SessionManager to flip IsEveryoneReady true.")]
        [SerializeField] private int _minPlayersToStart = 2;

        [Tooltip("Use Unity Relay for NAT punchthrough. Disable only for direct LAN testing.")]
        [SerializeField] private bool _useRelay = true;

        [Header("Transport Timeouts (ms, 0 = Unity default)")]
        [SerializeField] private int _connectTimeoutMs;
        [SerializeField] private int _disconnectTimeoutMs;
        [SerializeField] private int _heartbeatTimeoutMs;
        [SerializeField] private int _maxConnectAttempts;
        [SerializeField] private int _maxPayloadSize;

        [Header("Scenes (Optional)")]
        [Tooltip("Loaded by SessionManager when StartGame() succeeds. Leave empty to drive scenes from project code.")]
        [SerializeField] private string _gameSceneName;

        [Tooltip("Loaded by LobbyConnectionFlow after a successful host. Leave empty to drive scenes from project code.")]
        [SerializeField] private string _lobbySceneName;

        public int TickRate => _tickRate;
        public int MaxPlayers => _maxPlayers;
        public int MinPlayersToStart => _minPlayersToStart;
        public bool UseRelay => _useRelay;
        public int ConnectTimeoutMs => _connectTimeoutMs;
        public int DisconnectTimeoutMs => _disconnectTimeoutMs;
        public int HeartbeatTimeoutMs => _heartbeatTimeoutMs;
        public int MaxConnectAttempts => _maxConnectAttempts;
        public int MaxPayloadSize => _maxPayloadSize;
        public string GameSceneName => _gameSceneName;
        public string LobbySceneName => _lobbySceneName;
    }
}
