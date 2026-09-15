using System;
using System.Collections.Generic;

[Serializable]
public sealed class BackpackEquipInteractionModule : BackpackInteractionModule
{
    public override void CollectInteractionOptions(
        InventoryRuntime.Entry entry,
        List<BackpackInteractionOption> options)
    {
        options.Add(new BackpackInteractionOption(
            "equip_backpack",
            "装备背包",
            true,
            this,
            entry?.Item?.Definition?.Icon));
    }

    public override bool CanInteract(InventoryRuntime.Entry entry, Actor actor)
    {
        return entry != null &&
               actor?.inventorySystem != null &&
               FindBackpack(entry.Item) != null;
    }

    public override bool OnInteract(
        InventoryRuntime.Entry entry,
        Actor actor,
        string optionId)
    {
        if (optionId != "equip_backpack" ||
            actor == null ||
            !actor.IsServer ||
            !CanInteract(entry, actor))
        {
            return false;
        }

        return actor.inventorySystem.TryEquipBackpackFromInventory(
            entry.InstanceId,
            FindBackpack(entry.Item));
    }

    private static BackpackModule FindBackpack(ItemInstance item)
    {
        if (item?.Modules == null)
        {
            return null;
        }

        foreach (ItemModule module in item.Modules)
        {
            if (module is BackpackModule backpack)
            {
                return backpack;
            }
        }

        return null;
    }
}
