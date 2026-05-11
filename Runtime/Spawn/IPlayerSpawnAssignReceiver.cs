using UnityEngine;

namespace Emrgry.Network.Spawn
{
    /// <summary>
    /// Implemented by a player NetworkBehaviour to receive spawn-point assignments
    /// from <see cref="GameSceneSpawner"/>. The implementing component is expected
    /// to forward the assignment to its owner (e.g. via ClientRpc) and teleport on
    /// the owner side, since owner-authoritative NetworkTransforms forbid server-side
    /// teleports.
    /// </summary>
    public interface IPlayerSpawnAssignReceiver
    {
        /// <summary>Called on the server. Implementer should ClientRpc to its owner.</summary>
        void AssignSpawn(Vector3 position, Quaternion rotation);
    }
}
