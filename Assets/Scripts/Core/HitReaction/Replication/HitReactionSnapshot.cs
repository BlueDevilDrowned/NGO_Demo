using System;
using Unity.Netcode;

public struct HitReactionSnapshot : INetworkSerializable
{
    public const int MaxEvents=8;

    public uint Tick;
    public byte EventCount;
    public HitReactionEvent Event0;
    public HitReactionEvent Event1;
    public HitReactionEvent Event2;
    public HitReactionEvent Event3;
    public HitReactionEvent Event4;
    public HitReactionEvent Event5;
    public HitReactionEvent Event6;
    public HitReactionEvent Event7;

    public HitReactionEvent GetEvent(int index)
    {
        return index switch
        {
            0=>Event0, 1=>Event1, 2=>Event2, 3=>Event3,
            4=>Event4, 5=>Event5, 6=>Event6, 7=>Event7,
            _=>throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    public void SetEvent(int index,in HitReactionEvent reaction)
    {
        switch(index)
        {
            case 0: Event0=reaction; break;
            case 1: Event1=reaction; break;
            case 2: Event2=reaction; break;
            case 3: Event3=reaction; break;
            case 4: Event4=reaction; break;
            case 5: Event5=reaction; break;
            case 6: Event6=reaction; break;
            case 7: Event7=reaction; break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T:IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref EventCount);
        SerializeEvent(serializer,ref Event0);
        SerializeEvent(serializer,ref Event1);
        SerializeEvent(serializer,ref Event2);
        SerializeEvent(serializer,ref Event3);
        SerializeEvent(serializer,ref Event4);
        SerializeEvent(serializer,ref Event5);
        SerializeEvent(serializer,ref Event6);
        SerializeEvent(serializer,ref Event7);
    }

    private static void SerializeEvent<T>(
        BufferSerializer<T> serializer,
        ref HitReactionEvent reaction)
        where T:IReaderWriter
    {
        serializer.SerializeValue(ref reaction.Sequence);
        serializer.SerializeValue(ref reaction.Tick);
        serializer.SerializeValue(ref reaction.Location);
        serializer.SerializeValue(ref reaction.Direction);
        serializer.SerializeValue(ref reaction.WeaponType);
        serializer.SerializeValue(ref reaction.Damage);
    }
}
