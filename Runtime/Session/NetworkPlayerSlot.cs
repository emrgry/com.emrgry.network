using System;
using Unity.Collections;
using Unity.Netcode;

namespace Emrgry.Network.Session
{
    public struct NetworkPlayerSlot : INetworkSerializable, IEquatable<NetworkPlayerSlot>
    {
        public ulong ClientId;
        public ForceNetworkSerializeByMemcpy<FixedString64Bytes> DisplayName;
        public bool IsReady;
        public bool IsHost;
        public bool IsOccupied;
        public int SlotIndex;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref DisplayName);
            serializer.SerializeValue(ref IsReady);
            serializer.SerializeValue(ref IsHost);
            serializer.SerializeValue(ref IsOccupied);
            serializer.SerializeValue(ref SlotIndex);
        }

        public PlayerSlotData ToData(ulong localClientId)
        {
            return new PlayerSlotData
            {
                ClientId = ClientId,
                DisplayName = DisplayName.Value.ToString(),
                IsReady = IsReady,
                IsHost = IsHost,
                IsOccupied = IsOccupied,
                IsLocal = ClientId == localClientId,
                SlotIndex = SlotIndex
            };
        }

        public bool Equals(NetworkPlayerSlot other)
        {
            return ClientId == other.ClientId
                && DisplayName.Value.Equals(other.DisplayName.Value)
                && IsReady == other.IsReady
                && IsHost == other.IsHost
                && IsOccupied == other.IsOccupied
                && SlotIndex == other.SlotIndex;
        }

        public override int GetHashCode() => ClientId.GetHashCode();
    }
}
