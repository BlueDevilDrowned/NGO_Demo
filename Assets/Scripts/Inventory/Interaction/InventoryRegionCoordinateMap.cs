using UnityEngine;

public readonly struct InventoryRegionCoordinateMap
{
    public readonly Vector2 Origin;
    public readonly float CellSize;
    public readonly float Spacing;
    public readonly Vector2 TopLeft;

    public InventoryRegionCoordinateMap(Vector2 origin, float cellSize, float spacing)
    {
        Origin = origin;
        TopLeft = origin;
        CellSize = cellSize;
        Spacing = spacing;
    }

    public Vector2Int ScreenToCell(Vector2 localPosition)
    {
        float step = CellSize + Spacing;
        return new Vector2Int(
            Mathf.RoundToInt((localPosition.x - Origin.x) / step),
            Mathf.RoundToInt((Origin.y - localPosition.y) / step));
    }

    public Vector2 CellToLocal(Vector2Int cell)
    {
        float step = CellSize + Spacing;
        return new Vector2(Origin.x + CellSize * 0.5f + cell.x * step, Origin.y - CellSize * 0.5f - cell.y * step);
    }
}
