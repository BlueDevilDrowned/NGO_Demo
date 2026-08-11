using UnityEngine;

public struct HitReactionEvent
{
    public uint Sequence;
    public uint Tick;
    public HitLocation Location;
    public Vector3 Direction;
    public WeaponType WeaponType;
    public float Damage;
}
