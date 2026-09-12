using UnityEngine;

public readonly struct InventoryGridMetrics
{
    public readonly float CellSize;
    public readonly float Spacing;
    public readonly float Step;

    public InventoryGridMetrics(float cellSize, float spacing)
    {
        CellSize = cellSize;
        Spacing = spacing;
        Step = cellSize + spacing;
    }

    public Vector2 CellCenter(Vector2 topLeft, Vector2Int cell)
    {
        return topLeft + new Vector2(
            CellSize * 0.5f + cell.x * Step,
            -CellSize * 0.5f - cell.y * Step);
    }
}
