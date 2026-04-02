using System;
using Cysharp.Threading.Tasks;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Emrgry.Network.Connection
{
    public sealed class RelayAdapter : IRelayAdapter
    {
        private readonly string _region;

        public RelayAdapter(string region = "")
        {
            _region = region;
        }

        public async UniTask<RelayAllocationResult> CreateAllocationAsync(int maxPlayers)
        {
            try
            {
                // maxPlayers - 1 because host counts as a connection
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(
                    maxPlayers - 1,
                    string.IsNullOrEmpty(_region) ? null : _region);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                var serverData = allocation.ToRelayServerData("dtls");

                Debug.Log($"[RelayAdapter] Allocation created. Join code: {joinCode}");
                return new RelayAllocationResult(true, joinCode, serverData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayAdapter] Create allocation failed: {e.Message}");
                return RelayAllocationResult.Failed(e.Message);
            }
        }

        public async UniTask<RelayAllocationResult> JoinAllocationAsync(string joinCode)
        {
            try
            {
                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                var serverData = allocation.ToRelayServerData("dtls");

                Debug.Log($"[RelayAdapter] Joined allocation with code: {joinCode}");
                return new RelayAllocationResult(true, joinCode, serverData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[RelayAdapter] Join allocation failed: {e.Message}");
                return RelayAllocationResult.Failed(e.Message);
            }
        }
    }
}
