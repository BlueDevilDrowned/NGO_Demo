using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StorageShapeModule : ItemModule
{
    [Min(1)] public int GridWidth = 3;
    [Min(1)] public int GridHeight = 3;
    public bool CanRotate = true;
    public List<Vector2Int> Cells = new List<Vector2Int>();

    public override void CollectInteractionOptions(InventoryItemDefinition item,List<ItemInteractionOption> options)
        => options.Add(new ItemInteractionOption("pickup","拾取 "+ItemName(item),true,this,item.Icon));
    public override bool CanInteract(InventoryItemDefinition item,Actor actor) => actor?.inventorySystem!=null;
    public override bool OnInteract(ItemInstance item,Actor actor,string optionId)
        => optionId=="pickup" && actor.IsServer && actor.inventorySystem.TryAutoPlace(item,out _);
}
