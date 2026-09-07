using System;
using System.Collections.Generic;

namespace InventorySolver
{
    /// <summary>
    /// 表示库存布局的密封类，不可被继承
    /// </summary>
    public sealed class InventoryLayout
    {
        /// <summary>
        /// 获取只读的区域布局列表
        /// </summary>
        public IReadOnlyList<RegionLayout> Regions { get; }

        public InventoryLayout(IReadOnlyList<RegionLayout> regions)
        {
            if (regions == null)
                throw new ArgumentNullException(nameof(regions));
            if (regions.Count == 0)
                throw new ArgumentException(
                    "Inventory must contain at least one region.",
                    nameof(regions));

            Regions = regions;
        }

        public RegionLayout GetRegion(int regionIndex)
        {
            if (regionIndex < 0 || regionIndex >= Regions.Count)
                throw new ArgumentOutOfRangeException(nameof(regionIndex));

            return Regions[regionIndex];
        }
    }
}
