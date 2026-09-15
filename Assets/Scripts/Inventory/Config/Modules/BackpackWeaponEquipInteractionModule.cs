using System;
using System.Collections.Generic;

[Serializable]
public sealed class BackpackWeaponEquipInteractionModule : BackpackInteractionModule
{
    public override void CollectInteractionOptions(
        InventoryRuntime.Entry entry,
        List<BackpackInteractionOption> options)
    {
        options.Add(new BackpackInteractionOption(
            "equip_weapon_from_backpack",
            "装备武器",
            true,
            this,
            entry?.Item?.Definition?.Icon));
    }

    public override bool CanInteract(InventoryRuntime.Entry entry, Actor actor)
    {
        WeaponModuleInfo info = entry?.Item?.Definition?.GetInfo<WeaponModuleInfo>();
        return actor?.weaponInventory != null &&
               WeaponCatalog.TryGetId(info?.Weapon, out _);
    }

    public override bool OnInteract(
        InventoryRuntime.Entry entry,
        Actor actor,
        string optionId)
    {
        if (optionId != "equip_weapon_from_backpack" ||
            actor == null ||
            !actor.IsServer ||
            !CanInteract(entry, actor))
        {
            return false;
        }

        WeaponSO weapon = entry.Item.Definition.GetInfo<WeaponModuleInfo>().Weapon;
        return WeaponCatalog.TryGetId(weapon, out ushort weaponId) &&
               actor.inventorySystem.TryEquipWeapon(entry.InstanceId, weaponId);
    }
}

