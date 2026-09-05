using System;
using System.Collections.Generic;

[Serializable]
public sealed class WeaponInventoryData
{
    public byte currentIndex;
    public List<ushort> weaponIds=new();

    public ushort GetWeaponId(int slot)
    {
        return slot>=0&&slot<weaponIds.Count
            ?weaponIds[slot]
            :(ushort)0;
    }
}
