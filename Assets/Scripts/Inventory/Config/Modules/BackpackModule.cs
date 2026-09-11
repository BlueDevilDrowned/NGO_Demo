using System;
using System.Collections.Generic;
using InventorySolver;
using UnityEngine;

[Serializable]
public sealed class BackpackModule : ItemModule
{
    [Tooltip("背包内部存储区域，与物品自身占格形状独立。可直接在这里添加多个区域。")]
    public List<InventoryRegionDefinition> Regions = new List<InventoryRegionDefinition>
    {
        new InventoryRegionDefinition()
    };

    public InventoryLayout CreateLayout()
    {
        if (Regions == null || Regions.Count == 0)
            throw new InvalidOperationException("背包至少需要一个区域。");
        var layouts = new List<RegionLayout>();
        foreach (InventoryRegionDefinition region in Regions)
        {
            if (region == null) throw new InvalidOperationException("背包存在空区域。");
            var valid = new bool[region.Width * region.Height];
            foreach (Vector2Int cell in region.EnabledCells)
                if (cell.x >= 0 && cell.y >= 0 && cell.x < region.Width && cell.y < region.Height)
                    valid[cell.y * region.Width + cell.x] = true;
            layouts.Add(new RegionLayout(region.Width, region.Height, Array.AsReadOnly(valid)));
        }
        return new InventoryLayout(layouts.AsReadOnly());
    }

    public override void CollectInteractionOptions(InventoryItemDefinition item,List<ItemInteractionOption> options)
        => options.Add(new ItemInteractionOption("equip_backpack","装备 "+ItemName(item),true,this,item.Icon));
    public override bool CanInteract(InventoryItemDefinition item,Actor actor)
        => actor?.inventorySystem!=null && Regions!=null && Regions.Count>0;
    public override bool OnInteract(ItemInstance item,Actor actor,string optionId)
        => optionId=="equip_backpack" && actor.IsServer && actor.inventorySystem.TryEquipBackpack(CreateLayout());
}
