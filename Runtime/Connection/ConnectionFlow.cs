using Cysharp.Threading.Tasks;
using Emrgry.Core;
using Unity.Netcode;
using UnityEngine;

namespace Emrgry.Network.Connection
{
    /// <summary>
    /// Static convenience helpers around <see cref="IConnectionService"/> for the
    /// common host / join / leave flows. Services are resolved from
    /// <see cref="ServiceLocator"/> at call time so the helper has no lifecycle
    /// dependencies and can be invoked from UI handlers or coroutines alike.
    ///
    /// <para>This helper is pure Relay/Connection — it does NOT touch any lobby system.
    /// For combined Relay+Lobby flows see <c>LobbyConnectionFlow</c> in
    /// <c>com.emrgry.lobby</c>.</para>
    /// </summary>
    public static class ConnectionFlow
    {
        /// <summary>
        /// Start a relay-backed host using the package <see cref="IConnectionService"/>.
        /// Returns the result struct so callers can extract the join code and surface
        /// errors. Optionally calls <see cref="INetworkSceneService.LoadScene"/> when
        /// <paramref name="lobbySceneName"/> is non-empty.
        /// </summary>
        public static async UniTask<ConnectionResult> HostRelayAsync(
            int maxPlayers,
            string lobbySceneName = null,
            bool useRelay = true)
        {
            if (!ServiceLocator.TryResolve<IConnectionService>(out var connection))
            {
                Debug.LogError("[ConnectionFlow] IConnectionService not registered.");
                return ConnectionResult.Failed("IConnectionService not registered.");
            }

            var result = await connection.StartHostAsync(new HostConfig(maxPlayers, useRelay));
            if (!result.Success) return result;

            if (!string.IsNullOrEmpty(lobbySceneName)
                && ServiceLocator.TryResolve<INetworkSceneService>(out var sceneService))
            {
                sceneService.LoadScene(lobbySceneName);
            }
            return result;
        }

        /// <summary>
        /// Join a relay-backed session using the supplied join code. Pure connection
        /// layer; if you have a lobby id and need to resolve the join code from it,
        /// see <c>LobbyConnectionFlow.JoinGameByIdAsync</c>.
        /// </summary>
        public static async UniTask<ConnectionResult> JoinRelayAsync(string joinCode, bool useRelay = true)
        {
            if (!ServiceLocator.TryResolve<IConnectionService>(out var connection))
            {
                Debug.LogError("[ConnectionFlow] IConnectionService not registered.");
                return ConnectionResult.Failed("IConnectionService not registered.");
            }
            return await connection.JoinAsync(new JoinConfig(joinCode, useRelay));
        }

        /// <summary>
        /// Gracefully disconnect from the relay. Safe to call when not connected.
        /// </summary>
        public static async UniTask LeaveAsync()
        {
            if (ServiceLocator.TryResolve<IConnectionService>(out var connection) && connection.IsConnected)
                await connection.DisconnectAsync();
        }

        /// <summary>
        /// Iterate all non-host connected clients and call <c>DisconnectClient</c> on
        /// each so they get a clean disconnect notification before the host tears down
        /// the transport. Safe no-op when not hosting.
        /// </summary>
        public static void DisconnectAllClientsAsHost(string reason = "Host left the session")
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsHost) return;

            // Snapshot ids — DisconnectClient modifies the underlying collection.
            var ids = new System.Collections.Generic.List<ulong>(nm.ConnectedClientsIds);
            foreach (ulong clientId in ids)
            {
                if (clientId == nm.LocalClientId) continue;
                nm.DisconnectClient(clientId, reason);
            }
        }
    }
}
