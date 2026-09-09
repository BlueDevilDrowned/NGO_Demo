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

    public override void CollectInteractionOptions(ItemInteractionContext context, List<ItemInteractionOption> options)
    {
        if (context?.Actor == null) return;
        options.Add(new ItemInteractionOption("pickup", "放入背包", true,
            () => context.Actor.TryPickup?.Invoke(context.Item)));
    }
}
