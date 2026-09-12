using System.Collections.Generic;
using UnityEngine;
using InventorySolver;

public static class InventoryGeometry
{
    public static Vector2Int Rotate(Vector2Int p, ERotation r, int w, int h) => r switch
    {
        ERotation.R90 => new Vector2Int(h - 1 - p.y, p.x),
        ERotation.R180 => new Vector2Int(w - 1 - p.x, h - 1 - p.y),
        ERotation.R270 => new Vector2Int(p.y, w - 1 - p.x),
        _ => p
    };

    public static List<Vector2Int> Occupied(StorageShapeModule shape, ERotation rotation)
    {
        var result = new List<Vector2Int>();
        if (shape?.Cells == null) return result;
        foreach (var cell in shape.Cells) result.Add(Rotate(cell, rotation, shape.GridWidth, shape.GridHeight));
        return result;
    }
}
