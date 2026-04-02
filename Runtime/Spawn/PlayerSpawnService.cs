using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Emrgry.Network.Spawn
{
    public sealed class PlayerSpawnService : IPlayerSpawnService
    {
        private readonly NetworkManager _networkManager;
        private readonly Dictionary<ulong, NetworkObject> _playerObjects = new();
        private readonly List<Transform> _spawnPoints = new();
        private int _nextSpawnIndex;

        public PlayerSpawnService(NetworkManager networkManager)
        {
            _networkManager = networkManager;
        }

        public void RegisterSpawnPoint(Transform point)
        {
            if (!_spawnPoints.Contains(point))
                _spawnPoints.Add(point);
        }

        public void UnregisterSpawnPoint(Transform point)
        {
            _spawnPoints.Remove(point);
        }

        public void SpawnPlayer(ulong clientId, int spawnIndex = -1)
        {
            if (!_networkManager.IsServer)
            {
                Debug.LogWarning("[PlayerSpawnService] Only server can spawn players.");
                return;
            }

            if (_playerObjects.ContainsKey(clientId))
                return;

            if (_networkManager.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
            {
                _playerObjects[clientId] = client.PlayerObject;
                return;
            }

            var playerPrefab = _networkManager.NetworkConfig.PlayerPrefab;
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawnService] No player prefab assigned in NetworkManager.");
                return;
            }

            var spawnPosition = GetSpawnPosition(spawnIndex);
            var spawnRotation = GetSpawnRotation(spawnIndex);

            var instance = Object.Instantiate(playerPrefab, spawnPosition, spawnRotation);
            var networkObject = instance.GetComponent<NetworkObject>();
            networkObject.SpawnAsPlayerObject(clientId);

            _playerObjects[clientId] = networkObject;
        }

        public void DespawnPlayer(ulong clientId)
        {
            if (!_playerObjects.TryGetValue(clientId, out var networkObject)) return;
            if (networkObject != null && networkObject.IsSpawned)
                networkObject.Despawn();
            _playerObjects.Remove(clientId);
        }

        public GameObject GetPlayerObject(ulong clientId)
        {
            return _playerObjects.TryGetValue(clientId, out var obj) && obj != null
                ? obj.gameObject
                : null;
        }

        public Vector3 GetSpawnPosition(int index)
        {
            if (_spawnPoints.Count == 0) return Vector3.zero;
            int i = index >= 0 ? index % _spawnPoints.Count : _nextSpawnIndex++ % _spawnPoints.Count;
            return _spawnPoints[i].position;
        }

        private Quaternion GetSpawnRotation(int index)
        {
            if (_spawnPoints.Count == 0) return Quaternion.identity;
            int i = index >= 0 ? index % _spawnPoints.Count : (_nextSpawnIndex - 1) % _spawnPoints.Count;
            return _spawnPoints[i].rotation;
        }
    }
}
