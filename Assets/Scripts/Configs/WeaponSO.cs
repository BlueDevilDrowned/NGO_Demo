using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponSO", menuName = "Scriptable Objects/WeaponSO")]
public class WeaponSO : ScriptableObject
{
    [Header("Identity")]
    public WeaponType Type;

    [Header("Server")]
    [Min(1)]public int FireRate=1;
    [Min(0.01f)]public float Range=100f;
    [Min(0f)]public float ProjectileGravity=9.81f;
    public LayerMask HitMask=~0;
    public float ReloadDuration;
    public float Damage;
    public float SpreadDistance;

    [Header("Presentation")]
    [Min(0.01f)]public float TracerSpeed=200f;
    public WeaponTracerEffect TracerPrefab;
    public WeaponBulletMarkEffect BulletMarkPrefab;
    [Min(0.01f)]public float BulletMarkLifetime=10f;
    public WeaponImpactPresentationRule[] ImpactRules;
    [Min(1)]public int PoolDefaultCapacity=8;
    [Min(1)]public int PoolMaxSize=64;

    public WeaponImpactPresentationRule GetImpactRule(int layer)
    {
        if(layer<0||layer>31||ImpactRules==null)return null;

        int layerBit=1<<layer;
        for(int i=0;i<ImpactRules.Length;i++)
        {
            WeaponImpactPresentationRule rule=ImpactRules[i];
            if(rule!=null&&(rule.Layers.value&layerBit)!=0)
                return rule;
        }

        return null;
    }
}

[Serializable]
public sealed class WeaponImpactPresentationRule
{
    public string Name;
    public LayerMask Layers;
    public bool EnableBulletMark;
    public ParticleSystem[] ParticlePrefabs;
    [Min(0f)]public float ParticleNormalOffset=0.01f;
    public Vector3 ParticleRotationOffset;
}

public enum WeaponType : byte
{
    Rifle,
    Pistol,
    Shotgun,
}
