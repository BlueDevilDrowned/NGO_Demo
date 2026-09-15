using System;
using System.Collections.Generic;

[Serializable]
public sealed class BackpackDropInteractionModule : BackpackInteractionModule
{
    public override void CollectInteractionOptions(
        InventoryRuntime.Entry entry,
        List<BackpackInteractionOption> options)
    {
        options.Add(new BackpackInteractionOption(
            "drop_item",
            "丢弃",
            true,
            this,
            entry?.Item?.Definition?.Icon));
    }

    public override bool CanInteract(InventoryRuntime.Entry entry, Actor actor)
    {
        return entry != null && actor?.inventorySystem != null;
    }

    public override bool OnInteract(
        InventoryRuntime.Entry entry,
        Actor actor,
        string optionId)
    {
        return optionId == "drop_item" &&
               actor != null &&
               actor.IsServer &&
               actor.inventorySystem.TryDropItem(entry.InstanceId);
    }
}
