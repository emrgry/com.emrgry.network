namespace Emrgry.Network
{
    public readonly struct NetworkConnectedEvent
    {
        public readonly bool AsHost;
        public readonly string JoinCode;

        public NetworkConnectedEvent(bool asHost, string joinCode = "")
        {
            AsHost = asHost;
            JoinCode = joinCode;
        }
    }

    public readonly struct NetworkDisconnectedEvent
    {
        public readonly DisconnectReason Reason;

        public NetworkDisconnectedEvent(DisconnectReason reason)
        {
            Reason = reason;
        }
    }

    public readonly struct SessionPhaseChangedEvent
    {
        public readonly SessionPhase NewPhase;
        public readonly SessionPhase PreviousPhase;

        public SessionPhaseChangedEvent(SessionPhase newPhase, SessionPhase previousPhase)
        {
            NewPhase = newPhase;
            PreviousPhase = previousPhase;
        }
    }

    public readonly struct PlayerSlotUpdatedEvent
    {
        public readonly PlayerSlotData Slot;

        public PlayerSlotUpdatedEvent(PlayerSlotData slot)
        {
            Slot = slot;
        }
    }

    public readonly struct PlayerKickedEvent
    {
        public readonly ulong ClientId;
        public readonly bool IsLocal;

        public PlayerKickedEvent(ulong clientId, bool isLocal)
        {
            ClientId = clientId;
            IsLocal = isLocal;
        }
    }

    public readonly struct SceneLoadProgressEvent
    {
        public readonly string SceneName;
        public readonly float Progress;

        public SceneLoadProgressEvent(string sceneName, float progress)
        {
            SceneName = sceneName;
            Progress = progress;
        }
    }

    /// <summary>
    /// Published by NetworkSceneService when OnLoadEventCompleted fires — i.e. all clients
    /// have finished loading the scene. Server-side only.
    /// </summary>
    public readonly struct SceneLoadCompletedEvent
    {
        public readonly string SceneName;

        public SceneLoadCompletedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
