using System;
using System.Collections.Generic;

namespace InventorySolver
{
/// <summary>
/// 表示一个区域布局的密封类，用于定义区域的尺寸和有效单元格
/// </summary>
    public sealed class RegionLayout
    {
    /// <summary>
    /// 获取区域的宽度
    /// </summary>
        public int Width { get; }
    /// <summary>
    /// 获取区域的高度
    /// </summary>
        public int Height { get; }
    /// <summary>
    /// 获取区域中有效单元格的只读列表
    /// </summary>
        public IReadOnlyList<bool> ValidCells { get; }

        public RegionLayout(
            int width,
            int height,
            IReadOnlyList<bool> validCells)
        {
            if (width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height));
            if (validCells == null)
                throw new ArgumentNullException(nameof(validCells));
            if (validCells.Count != width * height)
                throw new ArgumentException(
                    "Valid cell count must equal width * height.",
                    nameof(validCells));

            Width = width;
            Height = height;
            ValidCells = validCells;
        }

        public bool IsValid(Cell cell)
        {
            if (cell.X < 0 || cell.X >= Width ||
                cell.Y < 0 || cell.Y >= Height)
                return false;

            return ValidCells[cell.Y * Width + cell.X];
        }
    }
}
