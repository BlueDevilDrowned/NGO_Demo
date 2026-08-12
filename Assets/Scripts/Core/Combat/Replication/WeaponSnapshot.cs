using System;
using Unity.Netcode;

public struct WeaponSnapshot : INetworkSerializable
{
    public const int MaxEvents=8;

    public uint Tick;
    public ushort EquippedWeaponId;
    public byte EventCount;
    //由于一个Tick可能多个事件，所以我们选择一次保存最近的8个状态
    public ShotData Event0;
    public ShotData Event1;
    public ShotData Event2;
    public ShotData Event3;
    public ShotData Event4;
    public ShotData Event5;
    public ShotData Event6;
    public ShotData Event7;

    public ShotData GetEvent(int index)
    {
        return index switch
        {
            0=>Event0,
            1=>Event1,
            2=>Event2,
            3=>Event3,
            4=>Event4,
            5=>Event5,
            6=>Event6,
            7=>Event7,
            _=>throw new ArgumentOutOfRangeException(nameof(index)),
        };
    }

    public void SetEvent(int index,in ShotData shot)
    {
        switch(index)
        {
            case 0: Event0=shot; break;
            case 1: Event1=shot; break;
            case 2: Event2=shot; break;
            case 3: Event3=shot; break;
            case 4: Event4=shot; break;
            case 5: Event5=shot; break;
            case 6: Event6=shot; break;
            case 7: Event7=shot; break;
            default: throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref EquippedWeaponId);
        serializer.SerializeValue(ref EventCount);
        SerializeShot(serializer,ref Event0);
        SerializeShot(serializer,ref Event1);
        SerializeShot(serializer,ref Event2);
        SerializeShot(serializer,ref Event3);
        SerializeShot(serializer,ref Event4);
        SerializeShot(serializer,ref Event5);
        SerializeShot(serializer,ref Event6);
        SerializeShot(serializer,ref Event7);
    }

    private static void SerializeShot<T>(
        BufferSerializer<T> serializer,
        ref ShotData shot)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref shot.Sequence);
        serializer.SerializeValue(ref shot.ProjectileId);
        serializer.SerializeValue(ref shot.ShotTick);
        serializer.SerializeValue(ref shot.EventTick);
        serializer.SerializeValue(ref shot.FireIntervalTicks);
        serializer.SerializeValue(ref shot.WeaponId);
        serializer.SerializeValue(ref shot.WeaponType);
        serializer.SerializeValue(ref shot.EventType);
        serializer.SerializeValue(ref shot.TracerSpeed);
        serializer.SerializeValue(ref shot.Gravity);
        serializer.SerializeValue(ref shot.Range);
        serializer.SerializeValue(ref shot.Origin);
        serializer.SerializeValue(ref shot.EndPoint);
        serializer.SerializeValue(ref shot.HasHit);
        serializer.SerializeValue(ref shot.HitLayer);
        serializer.SerializeValue(ref shot.HitNormal);
    }
}
