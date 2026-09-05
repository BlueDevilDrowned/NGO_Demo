using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Config/Weapon Inventory")]
public sealed class WeaponInventorySO : ScriptableObject
{
    public List<WeaponSlotConfig> Slots=new();
}
[Serializable]
public sealed class WeaponSlotConfig
{
        public WeaponSlotType Type;
        public bool CanDrop=true;
        [Min(0)]public int InitialWeaponId;
}
public enum WeaponSlotType : byte
{
    Melee,
    Primary,
}
