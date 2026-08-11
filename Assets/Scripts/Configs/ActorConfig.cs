using System;
using UnityEngine;

[Serializable]
public sealed class HitLocationDamageProfile
{
    [Min(0f)]public float Unknown=1f;
    [Min(0f)]public float Head=1f;
    [Min(0f)]public float Neck=1f;
    [Min(0f)]public float Chest=1f;
    [Min(0f)]public float Abdomen=1f;
    [Min(0f)]public float Pelvis=1f;
    [Min(0f)]public float LeftUpperArm=1f;
    [Min(0f)]public float RightUpperArm=1f;
    [Min(0f)]public float LeftForearm=1f;
    [Min(0f)]public float RightForearm=1f;
    [Min(0f)]public float LeftHand=1f;
    [Min(0f)]public float RightHand=1f;
    [Min(0f)]public float LeftThigh=1f;
    [Min(0f)]public float RightThigh=1f;
    [Min(0f)]public float LeftLowerLeg=1f;
    [Min(0f)]public float RightLowerLeg=1f;
    [Min(0f)]public float LeftFoot=1f;
    [Min(0f)]public float RightFoot=1f;

    public float Get(HitLocation location)
    {
        float multiplier=location switch
        {
            HitLocation.Head=>Head,
            HitLocation.Neck=>Neck,
            HitLocation.Chest=>Chest,
            HitLocation.Abdomen=>Abdomen,
            HitLocation.Pelvis=>Pelvis,
            HitLocation.LeftUpperArm=>LeftUpperArm,
            HitLocation.RightUpperArm=>RightUpperArm,
            HitLocation.LeftForearm=>LeftForearm,
            HitLocation.RightForearm=>RightForearm,
            HitLocation.LeftHand=>LeftHand,
            HitLocation.RightHand=>RightHand,
            HitLocation.LeftThigh=>LeftThigh,
            HitLocation.RightThigh=>RightThigh,
            HitLocation.LeftLowerLeg=>LeftLowerLeg,
            HitLocation.RightLowerLeg=>RightLowerLeg,
            HitLocation.LeftFoot=>LeftFoot,
            HitLocation.RightFoot=>RightFoot,
            _=>Unknown,
        };
        return Mathf.Max(0f,multiplier);
    }
}

[CreateAssetMenu(fileName="ActorConfig",menuName="Scriptable Objects/Actor Config")]
public sealed class ActorConfig : ScriptableObject
{
    [Header("Health")]
    [SerializeField,Min(1f)]private float maxHealth=100f;
    [Header("Hit Location Damage Multipliers")]
    [SerializeField]private HitLocationDamageProfile damageMultipliers=new();

    public float MaxHealth=>Mathf.Max(1f,maxHealth);

    public float GetDamageMultiplier(HitLocation location)
    {
        return damageMultipliers?.Get(location)??1f;
    }
}
