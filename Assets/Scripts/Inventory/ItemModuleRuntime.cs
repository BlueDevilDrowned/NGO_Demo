using System;
using System.Collections.Generic;

public interface IItemModuleRuntime
{
    void CollectInteractionOptions(ItemInteractionContext context, List<ItemInteractionOption> options);
}

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
    public Action<ItemInstance, InventoryLayoutDefinition> EquipBackpack;
}

public sealed class ItemInstance
{
    public InventoryItemDefinition Definition { get; }
    public IReadOnlyList<IItemModuleRuntime> Modules { get; }

    public ItemInstance(InventoryItemDefinition definition)
    {
        Definition = definition;
        var modules = new List<IItemModuleRuntime>();
        if (definition != null && definition.Modules != null)
            foreach (ItemModuleDefinition module in definition.Modules)
                if (module != null) modules.Add(module.CreateRuntime());
        Modules = modules.AsReadOnly();
    }
}
