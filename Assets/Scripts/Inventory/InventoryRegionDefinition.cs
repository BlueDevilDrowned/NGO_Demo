using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>背包的一个独立存储区域；坐标以该区域左下角为原点。</summary>
[Serializable]
public sealed class InventoryRegionDefinition
{
    public string DisplayName = "区域";
    [Min(1)] public int Width = 3;
    [Min(1)] public int Height = 3;

    [Tooltip("只有列表内的格子可以存放物品。每个区域使用独立的局部坐标。")]
    public List<Vector2Int> EnabledCells = new List<Vector2Int>
    {
        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1),
        new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2)
    };
}
