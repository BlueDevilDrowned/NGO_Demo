using UnityEngine;

public enum HitLocation : byte
{
    Unknown,
    Head,
    Neck,
    Chest,
    Abdomen,
    Pelvis,
    LeftUpperArm,
    RightUpperArm,
    LeftForearm,
    RightForearm,
    LeftHand,
    RightHand,
    LeftThigh,
    RightThigh,
    LeftLowerLeg,
    RightLowerLeg,
    LeftFoot,
    RightFoot,
}

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class Hitbox : MonoBehaviour
{
    [SerializeField]private HitLocation location=HitLocation.Unknown;

    private Collider hitCollider;

    [SerializeField]public HitboxManager Manager;
    public HitLocation Location=>location;
    public float DamageMultiplier=>Manager!=null
        ?Manager.GetDamageMultiplier(location)
        :1f;
    public Collider Collider=>hitCollider!=null
        ?hitCollider
        :hitCollider=GetComponent<Collider>();

    private void Awake()
    {
        hitCollider=GetComponent<Collider>();
    }

    private void Reset()
    {
        hitCollider=GetComponent<Collider>();
    }

    private void OnValidate()
    {
        hitCollider=GetComponent<Collider>();
    }

    internal void Bind(HitboxManager manager)
    {
        Manager=manager;
    }
}
