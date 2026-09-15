using System.Collections.Generic;
using InventorySolver;
using UnityEngine;

public static class InventoryGeometry
{
    // Logical shape coordinates are normalized by the solver before rendering.
    public static IReadOnlyList<Vector2Int> Occupied(StorageShapeModule shape, ERotation rotation)
    {
        List<Vector2Int> result = new List<Vector2Int>();
        if (shape == null || shape.Cells == null || shape.Cells.Count == 0)
        {
            result.Add(Vector2Int.zero);
            return result;
        }

        List<Cell> source = new List<Cell>(shape.Cells.Count);
        for (int i = 0; i < shape.Cells.Count; i++)
        {
            Vector2Int cell = shape.Cells[i];
            source.Add(new Cell(cell.x, cell.y));
        }

        ERotation appliedRotation = shape.CanRotate ? rotation : ERotation.R0;
        List<Cell> rotated = ShapeRotator.Rotate(new Shape(source), appliedRotation);
        for (int i = 0; i < rotated.Count; i++)
        {
            result.Add(new Vector2Int(rotated[i].X, rotated[i].Y));
        }

        return result;
    }

    public static RectInt Bounds(IReadOnlyList<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
        {
            return new RectInt(0, 0, 1, 1);
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int cell = cells[i];
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    /// <summary>
    /// Returns the rotated logical top-left anchor relative to the canonical
    /// anchor at (0, 0). The result may point to a virtual cell.
    /// </summary>
    public static Vector2Int RotationAnchorOffset(
        StorageShapeModule shape,
        ERotation rotation)
    {
        if (shape == null || !shape.CanRotate)
        {
            return Vector2Int.zero;
        }

        IReadOnlyList<Vector2Int> cells = Occupied(shape, ERotation.R0);
        if (cells == null || cells.Count == 0 || rotation == ERotation.R0)
        {
            return Vector2Int.zero;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        for (int i = 0; i < cells.Count; i++)
        {
            Vector2Int rotated = RotateRaw(cells[i], rotation);
            minX = Mathf.Min(minX, rotated.x);
            minY = Mathf.Min(minY, rotated.y);
        }

        return new Vector2Int(minX, minY);
    }

    private static Vector2Int RotateRaw(Vector2Int cell, ERotation rotation)
    {
        switch (rotation)
        {
            case ERotation.R0:
                return cell;
            case ERotation.R90:
                return new Vector2Int(-cell.y, cell.x);
            case ERotation.R180:
                return new Vector2Int(-cell.x, -cell.y);
            case ERotation.R270:
                return new Vector2Int(cell.y, -cell.x);
            default:
                return cell;
        }
    }
}
