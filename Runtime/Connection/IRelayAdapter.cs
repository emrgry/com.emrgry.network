using Cysharp.Threading.Tasks;
using Unity.Networking.Transport.Relay;

namespace Emrgry.Network.Connection
{
    public readonly struct RelayAllocationResult
    {
        public readonly bool Success;
        public readonly string JoinCode;
        public readonly RelayServerData ServerData;
        public readonly string ErrorMessage;

        public RelayAllocationResult(bool success, string joinCode, RelayServerData serverData, string errorMessage = "")
        {
            Success = success;
            JoinCode = joinCode;
            ServerData = serverData;
            ErrorMessage = errorMessage;
        }

        public static RelayAllocationResult Failed(string error) =>
            new(false, "", default, error);
    }

    public interface IRelayAdapter
    {
        UniTask<RelayAllocationResult> CreateAllocationAsync(int maxPlayers);
        UniTask<RelayAllocationResult> JoinAllocationAsync(string joinCode);
    }
}
