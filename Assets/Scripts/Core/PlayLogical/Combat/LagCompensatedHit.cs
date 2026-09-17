using UnityEngine;

public enum LagCompensatedHitType : byte
{
    None,
    Static,
    Hitbox,
    Pickup
}

public readonly struct LagCompensatedHit
{
    public LagCompensatedHitType Type { get; }
    public LagCompensatedBody Body { get; }
    public Collider SourceCollider { get; }
    public Hitbox Hitbox { get; }
    public Vector3 Point { get; }
    public Vector3 Normal { get; }
    public float Distance { get; }

    public LagCompensatedHit(
        LagCompensatedHitType type,
        LagCompensatedBody body,
        Collider sourceCollider,
        Hitbox hitbox,
        Vector3 point,
        Vector3 normal,
        float distance)
    {
        Type = type;
        Body = body;
        SourceCollider = sourceCollider;
        Hitbox = hitbox;
        Point = point;
        Normal = normal;
        Distance = distance;
    }
}
