using UnityEngine;

namespace Emrgry.Network
{
    public interface IPlayerSpawnService
    {
        void SpawnPlayer(ulong clientId, int spawnIndex = -1);
        void DespawnPlayer(ulong clientId);
        GameObject GetPlayerObject(ulong clientId);
        Vector3 GetSpawnPosition(int index);
        void RegisterSpawnPoint(Transform point);
        void UnregisterSpawnPoint(Transform point);
    }
}
