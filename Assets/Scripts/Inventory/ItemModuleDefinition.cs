using System.Collections.Generic;
using UnityEngine;

public abstract class ItemModuleDefinition : ScriptableObject
{
    public abstract IItemModuleRuntime CreateRuntime();
}

[CreateAssetMenu(menuName = "Inventory/Modules/Backpack", fileName = "BackpackModule")]
public sealed class BackpackModuleDefinition : ItemModuleDefinition
{
    public InventoryLayoutDefinition Layout;
    public override IItemModuleRuntime CreateRuntime() => new BackpackModuleRuntime(Layout);
}

public sealed class BackpackModuleRuntime : IItemModuleRuntime
{
    public InventoryLayoutDefinition Layout { get; }
    public BackpackModuleRuntime(InventoryLayoutDefinition layout) { Layout = layout; }

    public void CollectInteractionOptions(ItemInteractionContext context, List<ItemInteractionOption> options)
    {
        if (Layout == null || context == null || context.Actor == null) return;
        options.Add(new ItemInteractionOption("equip_backpack", "装备背包",
            context.Actor.CanEquipBackpack,
            () => context.Actor.EquipBackpack?.Invoke(context.Item, Layout)));
    }
}
