using System;
using Cysharp.Threading.Tasks;

namespace Emrgry.Network
{
    public enum ConnectionRole { Host, Client }

    public enum DisconnectReason
    {
        Intentional,
        HostLeft,
        Timeout,
        Kicked,
        ServerFull,
        VersionMismatch,
        ConnectionFailed,
        Unknown
    }

    public readonly struct HostConfig
    {
        public readonly int MaxPlayers;
        public readonly bool UseRelay;

        public HostConfig(int maxPlayers, bool useRelay = true)
        {
            MaxPlayers = maxPlayers;
            UseRelay = useRelay;
        }
    }

    public readonly struct JoinConfig
    {
        public readonly string JoinCode;
        public readonly bool UseRelay;

        public JoinConfig(string joinCode, bool useRelay = true)
        {
            JoinCode = joinCode;
            UseRelay = useRelay;
        }
    }

    public readonly struct ConnectionResult
    {
        public readonly bool Success;
        public readonly string JoinCode;
        public readonly string ErrorMessage;

        public ConnectionResult(bool success, string joinCode = "", string errorMessage = "")
        {
            Success = success;
            JoinCode = joinCode;
            ErrorMessage = errorMessage;
        }

        public static ConnectionResult Succeeded(string joinCode = "") => new(true, joinCode);
        public static ConnectionResult Failed(string error) => new(false, errorMessage: error);
    }

    public interface IConnectionService
    {
        bool IsHost { get; }
        bool IsConnected { get; }
        ConnectionRole Role { get; }
        string CurrentJoinCode { get; }

        UniTask<ConnectionResult> StartHostAsync(HostConfig config);
        UniTask<ConnectionResult> JoinAsync(JoinConfig config);
        UniTask DisconnectAsync();

        event Action<DisconnectReason> Disconnected;
        event Action<ulong> ClientConnected;
        event Action<ulong> ClientDisconnected;
    }
}
