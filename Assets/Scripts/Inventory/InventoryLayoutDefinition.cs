using System;
using System.Collections.Generic;
using InventorySolver;
using UnityEngine;

/// <summary>
/// 背包内部容量的静态配置。仅定义区域和可用格子，不保存运行时物品或全局 ID。
/// 背包物品的外部占用形状仍由 InventoryItemDefinition 定义。
/// </summary>
[CreateAssetMenu(menuName = "Inventory/Layout Definition", fileName = "InventoryLayoutDefinition")]
public sealed class InventoryLayoutDefinition : ScriptableObject
{
    [Tooltip("列表顺序对应求解器 RegionIndex。区域之间不能跨区放置同一个物品。")]
    public List<InventoryRegionDefinition> Regions = new List<InventoryRegionDefinition>
    {
        new InventoryRegionDefinition()
    };

    /// <summary>验证区域尺寸、启用格子及重复坐标，不修改配置。</summary>
    /// <param name="error">失败时包含区域索引的原因，成功时为空。</param>
    /// <returns>配置是否可转换为求解器布局。</returns>
    public bool TryValidate(out string error)
    {
        error = null;
        if (Regions == null || Regions.Count == 0)
        {
            error = "背包至少需要一个区域。";
            return false;
        }
        for (int i = 0; i < Regions.Count; i++)
        {
            InventoryRegionDefinition region = Regions[i];
            if (region == null || region.Width <= 0 || region.Height <= 0 ||
                (long)region.Width * region.Height > int.MaxValue)
            {
                error = $"区域 {i} 为空或尺寸无效。";
                return false;
            }
            if (region.EnabledCells == null || region.EnabledCells.Count == 0)
            {
                error = $"区域 {i} 至少需要一个启用格子。";
                return false;
            }
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int cell in region.EnabledCells)
            {
                if (cell.x < 0 || cell.y < 0 || cell.x >= region.Width || cell.y >= region.Height)
                {
                    error = $"区域 {i} 的格子 {cell} 超出了宽高范围。";
                    return false;
                }
                if (!seen.Add(cell))
                {
                    error = $"区域 {i} 的格子 {cell} 重复。";
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>创建独立的纯 C# 布局快照，供装备背包时交给运行时系统。</summary>
    /// <returns>不共享配置列表的求解器布局。</returns>
    /// <exception cref="InvalidOperationException">配置不合法。</exception>
    public InventoryLayout CreateLayout()
    {
        if (!TryValidate(out string error)) throw new InvalidOperationException(error);
        var regions = new List<RegionLayout>(Regions.Count);
        foreach (InventoryRegionDefinition region in Regions)
        {
            var validCells = new bool[region.Width * region.Height];
            foreach (Vector2Int cell in region.EnabledCells)
                validCells[cell.y * region.Width + cell.x] = true;
            regions.Add(new RegionLayout(region.Width, region.Height, Array.AsReadOnly(validCells)));
        }
        return new InventoryLayout(regions.AsReadOnly());
    }
}
