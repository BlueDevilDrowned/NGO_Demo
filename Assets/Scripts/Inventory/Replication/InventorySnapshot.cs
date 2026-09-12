using System;
using Unity.Collections;
using Unity.Netcode;
using InventorySolver;

public struct InventorySnapshot : INetworkSerializable
{
    public const byte MaxEntries = 32;
    public struct Entry
    {
        public int InstanceId;
        public FixedString64Bytes ItemId;
        public byte RegionIndex;
        public short AnchorX;
        public short AnchorY;
        public byte Rotation;
    }

    public uint ProcessedInputTick;
    public byte EntryCount;
    public Entry[] Entries;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ProcessedInputTick);
        serializer.SerializeValue(ref EntryCount);
        if (serializer.IsReader) Entries = new Entry[EntryCount];
        int count = Math.Min(EntryCount, MaxEntries);
        for (int i = 0; i < count; i++)
        {
            Entry entry = Entries[i];
            serializer.SerializeValue(ref entry.InstanceId);
            serializer.SerializeValue(ref entry.ItemId);
            serializer.SerializeValue(ref entry.RegionIndex);
            serializer.SerializeValue(ref entry.AnchorX);
            serializer.SerializeValue(ref entry.AnchorY);
            serializer.SerializeValue(ref entry.Rotation);
            Entries[i] = entry;
        }
    }

    public static InventorySnapshot FromData(in InventoryData data, uint tick)
    {
        int count = Math.Min(data?.entries?.Count ?? 0, MaxEntries);
        InventorySnapshot snapshot = new InventorySnapshot
        {
            ProcessedInputTick = tick,
            EntryCount = (byte)count,
            Entries = new Entry[count]
        };

        for (int i = 0; i < count; i++)
        {
            InventoryData.Entry entry = data.entries[i];
            snapshot.Entries[i] = new Entry
            {
                InstanceId = entry.instanceId,
                ItemId = entry.itemId ?? string.Empty,
                RegionIndex = (byte)Math.Max(0, entry.placement.RegionIndex),
                AnchorX = (short)entry.placement.Anchor.X,
                AnchorY = (short)entry.placement.Anchor.Y,
                Rotation = (byte)entry.placement.Rotation
            };
        }

        return snapshot;
    }

    public InventoryData ToData()
    {
        InventoryData data = new InventoryData();
        if (Entries == null) return data;
        int count = Math.Min(EntryCount, Entries.Length);
        for (int i = 0; i < count; i++)
        {
            Entry entry = Entries[i];
            data.entries.Add(new InventoryData.Entry
            {
                instanceId = entry.InstanceId,
                itemId = entry.ItemId.ToString(),
                placement = new Placement(
                    entry.RegionIndex,
                    new Cell(entry.AnchorX, entry.AnchorY),
                    (ERotation)entry.Rotation)
            });
        }
        return data;
    }
}
