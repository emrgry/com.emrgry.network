using System.Collections;
using Emrgry.Core;
using Emrgry.Core.Camera;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Emrgry.Network.Sync
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PlayerNetworkBehaviour : NetworkBehaviour, IPlayerNetworkActions
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
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner)
            {
                if (_cameraRetry != null)
                    StopCoroutine(_cameraRetry);
                UnregisterCamera();
            }
        }

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

        public void RequestInteract()
        {
            if (!IsOwner) return;
            RequestInteractServerRpc();
        }

        public void RequestPickup(string itemId)
        {
            if (!IsOwner) return;
            RequestPickupServerRpc(itemId);
        }

        public void RequestDrop()
        {
            if (!IsOwner) return;
            RequestDropServerRpc();
        }

        [ServerRpc]
        private void SetDisplayNameServerRpc(string name)
        {
            _displayName.Value = new ForceNetworkSerializeByMemcpy<FixedString64Bytes>(new FixedString64Bytes(name));
        }

        [ServerRpc]
        private void RequestInteractServerRpc(ServerRpcParams rpcParams = default)
        {
            Debug.Log($"[PlayerNetworkBehaviour] Player {rpcParams.Receive.SenderClientId} requested interact.");
        }

        [ServerRpc]
        private void RequestPickupServerRpc(string itemId, ServerRpcParams rpcParams = default)
        {
            Debug.Log($"[PlayerNetworkBehaviour] Player {rpcParams.Receive.SenderClientId} requested pickup: {itemId}");
        }

        [ServerRpc]
        private void RequestDropServerRpc(ServerRpcParams rpcParams = default)
        {
            Debug.Log($"[PlayerNetworkBehaviour] Player {rpcParams.Receive.SenderClientId} requested drop.");
        }
    }
}
