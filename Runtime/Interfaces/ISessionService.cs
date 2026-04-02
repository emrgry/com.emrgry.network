using System;
using System.Collections.Generic;

namespace Emrgry.Network
{
    public enum SessionPhase
    {
        WaitingForPlayers,
        Lobby,
        CountingDown,
        Loading,
        InGame,
        PostGame
    }

    public interface ISessionService
    {
        SessionPhase Phase { get; }
        IReadOnlyList<PlayerSlotData> PlayerSlots { get; }
        int MaxPlayers { get; }
        bool IsEveryoneReady { get; }

        void SetLocalPlayerReady(bool ready);
        void SetLocalPlayerName(string name);

        // Host-only
        void KickPlayer(ulong clientId);
        void StartGame();

        event Action<SessionPhase> PhaseChanged;
        event Action<PlayerSlotData> PlayerSlotUpdated;
        event Action<ulong> PlayerKicked;
    }
}
