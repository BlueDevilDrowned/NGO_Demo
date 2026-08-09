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
    [Min(1)]public int PoolDefaultCapacity=8;
    [Min(1)]public int PoolMaxSize=64;
}

public enum WeaponType : byte
{
    Rifle,
    Pistol,
    Shotgun,
}
