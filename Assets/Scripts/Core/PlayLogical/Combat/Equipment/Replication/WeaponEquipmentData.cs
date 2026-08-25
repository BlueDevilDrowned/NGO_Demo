using System;

[Serializable]
public struct WeaponEquipmentData
{
    public int id;
    public static WeaponEquipmentData NoWeapon()
    {
        WeaponEquipmentData data;
        data.id=-1;
        return data;
    }
}