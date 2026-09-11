using System;
using UnityEngine;

[Serializable]
public sealed class WeaponModuleInfo : ItemModuleInfo
{
    public WeaponSO Weapon;
    public override Type TargetModuleType => typeof(WeaponModule);
    public override UnityEngine.Object LookupKey => Weapon;
}
