using System;
using System.Collections.Generic;
[Serializable]
public sealed class WeaponModule : ItemModule
{
    public override Type InfoType => typeof(WeaponModuleInfo);
    public override void CollectInteractionOptions(InventoryItemDefinition item,List<ItemInteractionOption> options)
        => options.Add(new ItemInteractionOption("equip_weapon","装备 "+ItemName(item),true,this,item.Icon));
    public override bool CanInteract(InventoryItemDefinition item,Actor actor)
        => actor?.weaponInventory!=null && WeaponCatalog.TryGetId(item.GetInfo<WeaponModuleInfo>()?.Weapon,out _);
    public override bool OnInteract(ItemInstance item,Actor actor,string optionId)
    {
        if(optionId!="equip_weapon" || !actor.IsServer || !CanInteract(item.Definition,actor)) return false;
        WeaponCatalog.TryGetId(item.Definition.GetInfo<WeaponModuleInfo>().Weapon,out ushort id);
        // Keep replacement transactional: reject if the old weapon cannot be dropped.
        return actor.weaponInventory.TryPickupFromItem(id);
    }
}
