using System;
using System.Collections.Generic;

public sealed class ItemInteractionContext
{
    public ItemInstance Item { get; }
    public ActorContext Actor { get; }
    public ItemInteractionContext(ItemInstance item, ActorContext actor) { Item = item; Actor = actor; }
}

// 业务层可替换为现有 Actor/Inventory 类型，核心模块不依赖 Unity 或网络代码。
public sealed class ActorContext
{
    public bool CanEquipBackpack;
    public Action<ItemInstance, InventorySolver.InventoryLayout> EquipBackpack;
    public Action<ItemInstance> TryPickup;
}

public sealed class ItemInstance
{
    public InventoryItemDefinition Definition { get; }
    public IReadOnlyList<ItemModule> Modules => Definition.Modules;

    public ItemInstance(InventoryItemDefinition definition)
    {
        Definition = definition ?? throw new ArgumentNullException(nameof(definition));
    }

    public void CollectInteractionOptions(ActorContext actor, List<ItemInteractionOption> options)
    {
        var context = new ItemInteractionContext(this, actor);
        if (Modules == null) return;
        foreach (ItemModule module in Modules)
            module?.CollectInteractionOptions(context, options);
    }
}
