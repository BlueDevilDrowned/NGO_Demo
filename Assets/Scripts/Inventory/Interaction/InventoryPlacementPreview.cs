using System.Collections.Generic;
using InventorySolver;
using UnityEngine;

public sealed class InventoryPlacementPreview
{
    public bool IsValid { get; private set; }
    public readonly List<Vector2Int> Cells = new List<Vector2Int>();
    public readonly List<Vector2Int> InvalidCells = new List<Vector2Int>();

    public void Evaluate(
        StorageShapeModule shape,
        Placement placement,
        InventoryRegionDefinition region,
        IReadOnlyList<InventoryRuntime.Entry> others,
        int ignoredInstanceId = -1)
    {
        Cells.Clear();
        InvalidCells.Clear();
        IsValid = shape != null && region != null;
        if (!IsValid)
        {
            return;
        }

        IReadOnlyList<Vector2Int> occupied = InventoryGeometry.Occupied(shape, placement.Rotation);
        HashSet<Vector2Int> enabled = new HashSet<Vector2Int>(region.EnabledCells ?? new List<Vector2Int>());
        HashSet<Vector2Int> blocked = BuildBlockedCells(others, placement.RegionIndex, ignoredInstanceId);
        for (int i = 0; i < occupied.Count; i++)
        {
            Vector2Int cell = occupied[i] + new Vector2Int(placement.Anchor.X, placement.Anchor.Y);
            Cells.Add(cell);
            bool valid = cell.x >= 0 && cell.y >= 0 &&
                cell.x < region.Width && cell.y < region.Height &&
                enabled.Contains(cell) && !blocked.Contains(cell);
            if (!valid)
            {
                InvalidCells.Add(cell);
                IsValid = false;
            }
        }
    }

    private static HashSet<Vector2Int> BuildBlockedCells(
        IReadOnlyList<InventoryRuntime.Entry> others,
        int regionIndex,
        int ignoredInstanceId)
    {
        HashSet<Vector2Int> blocked = new HashSet<Vector2Int>();
        if (others == null)
        {
            return blocked;
        }

        for (int i = 0; i < others.Count; i++)
        {
            InventoryRuntime.Entry entry = others[i];
            if (entry == null || entry.InstanceId == ignoredInstanceId || entry.Placement.RegionIndex != regionIndex)
            {
                continue;
            }

            StorageShapeModule shape = FindShape(entry.Item);
            IReadOnlyList<Vector2Int> cells = InventoryGeometry.Occupied(shape, entry.Placement.Rotation);
            for (int j = 0; j < cells.Count; j++)
            {
                Vector2Int cell = cells[j] + new Vector2Int(entry.Placement.Anchor.X, entry.Placement.Anchor.Y);
                blocked.Add(cell);
            }
        }

        return blocked;
    }

    private static StorageShapeModule FindShape(ItemInstance item)
    {
        if (item == null || item.Modules == null)
        {
            return null;
        }

        for (int i = 0; i < item.Modules.Count; i++)
        {
            if (item.Modules[i] is StorageShapeModule shape)
            {
                return shape;
            }
        }

        return null;
    }
}
