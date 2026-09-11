using System;
using System.Collections.Generic;
[Serializable]
public abstract class ItemModule
{
    public virtual Type InfoType => null;
    public virtual bool CanShow(InventoryItemDefinition item, Actor actor) => true;
    public virtual void OnShow(InventoryItemDefinition item, Actor actor) { }
    public virtual bool CanInteract(InventoryItemDefinition item, Actor actor) => false;
    public virtual bool OnInteract(ItemInstance item, Actor actor, string optionId) => false;
    public abstract void CollectInteractionOptions(InventoryItemDefinition item, List<ItemInteractionOption> options);
    protected static string ItemName(InventoryItemDefinition item) =>
        string.IsNullOrWhiteSpace(item.DisplayName) ? item.name : item.DisplayName;
}
