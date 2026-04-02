using Emrgry.Core;
using UnityEngine;

namespace Emrgry.Network.Spawn
{
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private int _priority;
        [SerializeField] private Color _gizmoColor = Color.green;

        public int Priority => _priority;

        private void OnEnable()
        {
            if (ServiceLocator.TryResolve<IPlayerSpawnService>(out var service))
                service.RegisterSpawnPoint(transform);
        }

        private void OnDisable()
        {
            if (ServiceLocator.TryResolve<IPlayerSpawnService>(out var service))
                service.UnregisterSpawnPoint(transform);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward);
        }
    }
}
