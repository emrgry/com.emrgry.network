using System.Collections;
using Emrgry.Core;
using Emrgry.Core.Camera;
using Emrgry.Network.Spawn;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Emrgry.Network.Sync
{
    /// <summary>
    /// Stock networked player component. Handles:
    /// <list type="bullet">
    /// <item><description><b>Display name</b> as a server-written <see cref="NetworkVariable{T}"/>.</description></item>
    /// <item><description><b>Camera target registration</b> with <see cref="ICameraModeService"/>,
    /// with a coroutine retry so late-join races (IsOwner true a frame after spawn) are
    /// handled.</description></item>
    /// <item><description><b>Spawn-point assignment</b> via <see cref="IPlayerSpawnAssignReceiver"/>:
    /// server calls <see cref="AssignSpawn"/>, the behaviour forwards to the owner via
    /// ClientRpc, and the owner teleports its own (owner-authoritative) NetworkTransform.</description></item>
    /// </list>
    /// Subclasses may extend by overriding the partial-method-style hooks
    /// <see cref="OnOwnerSpawned"/> and <see cref="OnOwnerDespawned"/>.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNetworkBehaviour : NetworkBehaviour, IPlayerSpawnAssignReceiver
    {
        private readonly NetworkVariable<ForceNetworkSerializeByMemcpy<FixedString64Bytes>> _displayName = new(
            new ForceNetworkSerializeByMemcpy<FixedString64Bytes>(new FixedString64Bytes("")),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private bool _registeredWithCamera;
        private Coroutine _cameraRetry;

        public string DisplayName => _displayName.Value.Value.ToString();

        public override void OnNetworkSpawn()
        {
            if (IsOwner)
            {
                SetDisplayNameServerRpc($"Player {OwnerClientId}");
                _cameraRetry = StartCoroutine(RegisterCameraWhenReady());
                OnOwnerSpawned();
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                if (_cameraRetry != null) StopCoroutine(_cameraRetry);
                UnregisterCamera();
                OnOwnerDespawned();
            }
        }

        /// <summary>Extension hook — runs once on the owning client after camera retry starts.</summary>
        protected virtual void OnOwnerSpawned() { }

        /// <summary>Extension hook — runs once on the owning client during despawn.</summary>
        protected virtual void OnOwnerDespawned() { }

        // ---- Spawn-point assignment (owner-authoritative transform safe) ----

        /// <inheritdoc />
        public void AssignSpawn(Vector3 position, Quaternion rotation)
        {
            if (!IsServer) return;
            AssignSpawnClientRpc(position, rotation,
                new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });
        }

        [ClientRpc]
        private void AssignSpawnClientRpc(Vector3 position, Quaternion rotation, ClientRpcParams _ = default)
        {
            if (!IsOwner) return;
            if (TryGetComponent<NetworkTransform>(out var nt))
                nt.Teleport(position, rotation, transform.localScale);
            else
                transform.SetPositionAndRotation(position, rotation);
        }

        // ---- Camera target registration ----

        private IEnumerator RegisterCameraWhenReady()
        {
            while (!_registeredWithCamera)
            {
                if (ServiceLocator.TryResolve<ICameraModeService>(out var cameraService))
                {
                    cameraService.AddTarget(transform);
                    _registeredWithCamera = true;
                    yield break;
                }
                yield return null;
            }
        }

        private void UnregisterCamera()
        {
            if (!_registeredWithCamera) return;
            if (!ServiceLocator.TryResolve<ICameraModeService>(out var cameraService)) return;
            cameraService.RemoveTarget(transform);
            _registeredWithCamera = false;
        }

        [ServerRpc]
        private void SetDisplayNameServerRpc(string name)
        {
            _displayName.Value = new ForceNetworkSerializeByMemcpy<FixedString64Bytes>(new FixedString64Bytes(name));
        }
    }
}
