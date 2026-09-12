using System.Collections.Generic;
using UnityEngine;
using InventorySolver;

public sealed class InventoryPlacementPreview
{
    public bool IsValid { get; private set; }
    public readonly List<Vector2Int> Cells = new();
    public void Evaluate(StorageShapeModule shape, Placement placement, InventoryRegionDefinition region, IReadOnlyList<InventoryRuntime.Entry> others)
    {
        Cells.Clear(); IsValid = shape != null && region != null;
        var occupied = InventoryGeometry.Occupied(shape, placement.Rotation);
        foreach (var c in occupied)
        {
            var p = new Vector2Int(placement.Anchor.X + c.x, placement.Anchor.Y + c.y); Cells.Add(p);
            if (!region.EnabledCells.Contains(p)) IsValid = false;
            if (p.x < 0 || p.y < 0 || p.x >= region.Width || p.y >= region.Height) IsValid = false;
        }
        if (others != null) foreach (var e in others) if (e != null)
            foreach (var c in InventoryGeometry.Occupied(FindShape(e.Item), e.Placement.Rotation))
                foreach (var p in Cells) if (p == new Vector2Int(e.Placement.Anchor.X+c.x,e.Placement.Anchor.Y+c.y)) IsValid=false;
    }
    private static StorageShapeModule FindShape(ItemInstance item)
    {
        if (item?.Modules == null)
            return null;
        foreach (var module in item.Modules)
        {
            if (module is StorageShapeModule shape)
                return shape;
        }
        return null;
    }
}
