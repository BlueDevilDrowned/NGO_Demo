using UnityEngine;

public readonly struct InventoryRegionCoordinateMap
{
    public readonly float CellSize;
    public readonly float Spacing;
    public readonly float Step;
    public readonly Vector2 TopLeft;

    public InventoryRegionCoordinateMap(float cellSize, float spacing)
    {
        CellSize = Mathf.Max(0.01f, cellSize);
        Spacing = Mathf.Max(0f, spacing);
        Step = CellSize + Spacing;
        TopLeft = Vector2.zero;
    }

    public Vector2 CellCenter(Vector2Int cell)
    {
        return new Vector2(
            CellSize * 0.5f + cell.x * Step,
            -CellSize * 0.5f - cell.y * Step);
    }

    public Vector2 CellToLocal(Vector2Int cell)
    {
        return CellCenter(cell);
    }

    public Vector2Int LocalToCell(Vector2 localPosition)
    {
        return new Vector2Int(
            Mathf.RoundToInt((localPosition.x - CellSize * 0.5f) / Step),
            Mathf.RoundToInt((-localPosition.y - CellSize * 0.5f) / Step));
    }

    public bool TryScreenToCell(RectTransform region, Vector2 screenPosition, Camera camera, out Vector2Int cell)
    {
        cell = default;
        if (region == null)
        {
            return false;
        }

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(region, screenPosition, camera, out local))
        {
            return false;
        }

        cell = LocalToCell(local);
        return true;
    }
}
