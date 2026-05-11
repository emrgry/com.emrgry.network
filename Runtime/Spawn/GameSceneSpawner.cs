using Emrgry.Core;
using Unity.Netcode;
using UnityEngine;

namespace Emrgry.Network.Spawn
{
    /// <summary>
    /// Server-side scene component that places connected players at registered
    /// <see cref="SpawnPoint"/> positions when the game scene loads. Each player's
    /// teleport is dispatched to that player's <i>owner</i> via the player's own
    /// <see cref="IPlayerSpawnAssignReceiver"/>, so this works with
    /// <c>NetworkTransformAuthority.Owner</c> setups.
    ///
    /// <para><b>Why owner-side teleport?</b> A server-side <c>NetworkTransform.Teleport</c>
    /// throws when the transform is owner-authoritative. The previous package version
    /// did exactly that and aborted the spawn loop, leaving remote players at the
    /// origin. v2.0 instead asks each player's <see cref="PlayerNetworkBehaviour"/> to
    /// teleport itself on the owner side via ClientRpc.</para>
    /// </summary>
    public sealed class GameSceneSpawner : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            if (!IsServer) return;

            var spawnService = ServiceLocator.Resolve<IPlayerSpawnService>();
            int spawnIndex = 0;

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)
                    && client.PlayerObject != null)
                {
                    AssignSpawnToExistingPlayer(client.PlayerObject, spawnService, spawnIndex);
                }
                else
                {
                    spawnService.SpawnPlayer(clientId, spawnIndex);
                }
                spawnIndex++;
            }

            NetworkManager.OnClientConnectedCallback += OnLateJoin;
        }

        public override void OnNetworkDespawn()
        {
            if (IsServer)
                NetworkManager.OnClientConnectedCallback -= OnLateJoin;
        }

        private static void AssignSpawnToExistingPlayer(NetworkObject playerObj, IPlayerSpawnService service, int index)
        {
            var position = service.GetSpawnPosition(index);

            // Owner-authoritative path: ask the player to teleport itself.
            if (playerObj.TryGetComponent<IPlayerSpawnAssignReceiver>(out var receiver))
            {
                receiver.AssignSpawn(position, Quaternion.identity);
                return;
            }

            // Fallback (server-authoritative transform): mutate the transform directly.
            var cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerObj.transform.SetPositionAndRotation(position, Quaternion.identity);
            if (cc != null) cc.enabled = true;
        }

        private void OnLateJoin(ulong clientId)
        {
            if (!IsServer) return;
            ServiceLocator.Resolve<IPlayerSpawnService>().SpawnPlayer(clientId);
        }
    }
}
