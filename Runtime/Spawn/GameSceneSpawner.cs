using Emrgry.Core;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Emrgry.Network.Spawn
{
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
                    var pos = spawnService.GetSpawnPosition(spawnIndex);
                    var playerObj = client.PlayerObject;

                    var cc = playerObj.GetComponent<CharacterController>();
                    if (cc != null) cc.enabled = false;

                    playerObj.transform.position = pos;
                    playerObj.transform.rotation = Quaternion.identity;

                    if (cc != null) cc.enabled = true;

                    var nt = playerObj.GetComponent<NetworkTransform>();
                    if (nt != null)
                        nt.Teleport(pos, Quaternion.identity, playerObj.transform.localScale);
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

        private void OnLateJoin(ulong clientId)
        {
            if (!IsServer) return;
            ServiceLocator.Resolve<IPlayerSpawnService>().SpawnPlayer(clientId);
        }
    }
}
