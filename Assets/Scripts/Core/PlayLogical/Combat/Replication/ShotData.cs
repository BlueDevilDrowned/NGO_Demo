using UnityEngine;

public enum ShotEventType : byte
{
    Spawn,
    Hit,
    Expired,
}

public struct ShotData
{
    public uint Sequence;
    public uint ProjectileId;
    public uint ShotTick;
    public uint EventTick;
    public uint FireIntervalTicks;
    public ushort WeaponId;
    public ShotEventType EventType;
    public float TracerSpeed;
    public float Gravity;
    public float Range;
    public Vector3 Origin;
    public Vector3 EndPoint;
    public bool HasHit;
    public byte HitLayer;
    public Vector3 HitNormal;
}
