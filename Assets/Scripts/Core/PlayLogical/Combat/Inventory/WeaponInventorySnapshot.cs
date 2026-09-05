using System;
using Unity.Netcode;

public struct WeaponInventorySnapshot : INetworkSerializable
{
    public const byte MaxSlots=8;

    public uint ProcessedInputTick;
    public byte SlotCount;
    public byte CurrentIndex;
    public ushort Slot0;
    public ushort Slot1;
    public ushort Slot2;
    public ushort Slot3;
    public ushort Slot4;
    public ushort Slot5;
    public ushort Slot6;
    public ushort Slot7;

    public ushort GetSlot(int index)
    {
        return index switch
        {
            0=>Slot0,
            1=>Slot1,
            2=>Slot2,
            3=>Slot3,
            4=>Slot4,
            5=>Slot5,
            6=>Slot6,
            7=>Slot7,
            _=>throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    public void SetSlot(int index,ushort weaponId)
    {
        switch(index)
        {
            case 0: Slot0=weaponId; break;
            case 1: Slot1=weaponId; break;
            case 2: Slot2=weaponId; break;
            case 3: Slot3=weaponId; break;
            case 4: Slot4=weaponId; break;
            case 5: Slot5=weaponId; break;
            case 6: Slot6=weaponId; break;
            case 7: Slot7=weaponId; break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public WeaponInventoryData ToData()
    {
        WeaponInventoryData data=new();
        data.currentIndex=CurrentIndex;
        int count=Math.Min(SlotCount,MaxSlots);
        for(int i=0;i<count;i++)
            data.weaponIds.Add(GetSlot(i));
        return data;
    }

    public static WeaponInventorySnapshot FromData(
        in WeaponInventoryData data,
        uint processedInputTick)
    {
        WeaponInventorySnapshot snapshot=new()
        {
            ProcessedInputTick=processedInputTick,
            SlotCount=(byte)Math.Min(
                data?.weaponIds?.Count??0,
                MaxSlots),
            CurrentIndex=data?.currentIndex??0,
        };

        for(int i=0;i<snapshot.SlotCount;i++)
            snapshot.SetSlot(i,data.weaponIds[i]);
        return snapshot;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref ProcessedInputTick);
        serializer.SerializeValue(ref SlotCount);
        serializer.SerializeValue(ref CurrentIndex);
        serializer.SerializeValue(ref Slot0);
        serializer.SerializeValue(ref Slot1);
        serializer.SerializeValue(ref Slot2);
        serializer.SerializeValue(ref Slot3);
        serializer.SerializeValue(ref Slot4);
        serializer.SerializeValue(ref Slot5);
        serializer.SerializeValue(ref Slot6);
        serializer.SerializeValue(ref Slot7);
    }
}
