namespace Emrgry.Network
{
    public struct PlayerSlotData
    {
        public ulong ClientId;
        public string DisplayName;
        public bool IsReady;
        public bool IsHost;
        public bool IsOccupied;
        public bool IsLocal;
        public int SlotIndex;
    }
}
