using System;
using System.Collections.Generic;

namespace InventorySolver
{
    public sealed class BacktrackingSolver : IInventorySolver
    {
        // 验证指定位置，不修改任何背包状态。
        public PlacementCheckResult CanPlace(
            InventoryLayout layout,
            IReadOnlyList<SolverItem> existingItems,
            SolverItem item,
            Placement placement)
        {
            var occupied = BuildOccupied(layout, existingItems, item.Token);
            return Check(layout, item, placement, occupied, out _);
        }

        // 先尝试空闲位置，失败后才从目标物品开始 blocker 回溯。
        public SolveResult TryAutoPlace(
            InventoryLayout layout,
            IReadOnlyList<SolverItem> existingItems,
            SolverItem item,
            SolverOptions options)
        {
            options = options ?? new SolverOptions();
            var items = new Dictionary<int, SolverItem>();
            //构建已存在物品
            foreach (SolverItem existing in existingItems)
                items.Add(existing.Token, existing);
            items[item.Token] = item;

            var placements = new Dictionary<int, Placement>();
            //构建放置方式
            foreach (SolverItem existing in existingItems)
                if (existing.CurrentPlacement.HasValue)
                    placements[existing.Token] = existing.CurrentPlacement.Value;

            var occupied = BuildOccupied(layout, existingItems, -1);
            int nodes = 0;

            // 第一阶段只尝试完全空闲的位置，正常放入不会触发重排，失败则用得到的候选列表继续第二阶段选取
            if (TryPlaceDirect(
                    layout, item, occupied,
                    out Placement direct, out List<Placement> candidates))
            {
                placements[item.Token] = direct;
                return BuildResult(items, placements, item.Token, nodes);
            }

            // 第二阶段从目标物品开始；只有它实际挡住的物品才进入递归。
            if (!SearchTarget(
                    layout, item, occupied, placements, items,
                    options, 0, ref nodes, candidates))
            {
                return new SolveResult(false, new List<SolvedItem>(), nodes);
            }

            return BuildResult(items, placements, item.Token, nodes);
        }

        // 为一个物品枚举候选位置；覆盖的物品会形成递归子问题。
        private bool SearchTarget(
            InventoryLayout layout,
            SolverItem item,
            Dictionary<RegionCell, int> occupied,
            Dictionary<int, Placement> placements,
            Dictionary<int, SolverItem> items,
            SolverOptions options,
            int depth,
            ref int nodes,
            IReadOnlyList<Placement> candidates = null)
        {
            if (++nodes > options.MaxNodes || depth > options.MaxDepth)
                return false;

            // 根物品复用首次扫描的几何候选；占用随分支变化，必须重新查询。
            IEnumerable<Placement> positions = candidates;
            //对于没有生成后续选列表，则采用迭代器，挨个生成，这样找到合适位置直接返回，不再生成后续候选位置
            foreach (Placement candidate in positions)
            {
                var occupiedSnapshot = new Dictionary<RegionCell, int>(occupied);
                var placementsSnapshot = new Dictionary<int, Placement>(placements);
                //获得覆盖物品的token
                List<int> blockerTokens = CollectBlockers(item, candidate, occupied);
                //获取覆盖物品信息
                var removed = new List<SolverItem>();
                foreach (int token in blockerTokens)
                {
                    SolverItem blocker = items[token];
                    //物品不能移动就直接跳过这个候选位置
                    if (!blocker.CanMove)
                    {
                        removed.Clear();
                        break;
                    }
                    //把placements中放置的物品，放进removed中
                    Placement old = placements[token];
                    RemoveCells(occupied, blocker, old);
                    placements.Remove(token);
                    removed.Add(blocker); 
                }
                //数量不一致时，说明有问题，恢复并跳过这个侯选位置
                if (blockerTokens.Count != removed.Count)
                { RestoreSnapshot(occupied, placements, occupiedSnapshot, placementsSnapshot); }

                if (blockerTokens.Count != removed.Count)
                    continue;
                //

                //把目标物品放入候选位置
                AddCells(occupied, item, candidate);
                placements[item.Token] = candidate;
                bool success = Reinsert(
                    layout, removed, 0, occupied, placements,
                    items, options, depth + 1, ref nodes);
                if (success)
                    return true;
                RestoreSnapshot(occupied, placements, occupiedSnapshot, placementsSnapshot);
            }
            return false;
        }

        /// <summary>恢复某个搜索分支开始时的完整占用和位置状态。</summary>
        private static void RestoreSnapshot(Dictionary<RegionCell, int> occupied, Dictionary<int, Placement> placements,
            Dictionary<RegionCell, int> occupiedSnapshot, Dictionary<int, Placement> placementsSnapshot)
        {
            occupied.Clear();
            foreach (var pair in occupiedSnapshot) occupied[pair.Key] = pair.Value;
            placements.Clear();
            foreach (var pair in placementsSnapshot) placements[pair.Key] = pair.Value;
        }

        // 重新安置当前候选位置移出的 blocker，失败时由上层回滚。
        private bool Reinsert(
            InventoryLayout layout,
            List<SolverItem> blockers,
            int index,
            Dictionary<RegionCell, int> occupied,
            Dictionary<int, Placement> placements,
            Dictionary<int, SolverItem> items,
            SolverOptions options,
            int depth,
            ref int nodes)
        {
            if (index == blockers.Count)
                return true;

            SolverItem blocker = blockers[index];
            //能找到位置，继续递归，子问题返回false
            // blocker 的重新安置只能尝试当前分支中的空闲位置。
            // 不能调用 SearchTarget，否则会把本分支已经放置的物品再次作为 blocker 搬走。
            foreach (Placement direct in EnumerateCandidates(layout, blocker))
            {
                var occupiedSnapshot = new Dictionary<RegionCell, int>(occupied);
                var placementsSnapshot = new Dictionary<int, Placement>(placements);
                if (CollectBlockers(blocker, direct, occupied).Count != 0)
                    continue;
                AddCells(occupied, blocker, direct);
                placements[blocker.Token] = direct;
                bool success = Reinsert(
                    layout, blockers, index + 1, occupied, placements,
                    items, options, depth, ref nodes);
                if (success) return true;
                RestoreSnapshot(occupied, placements, occupiedSnapshot, placementsSnapshot);
            }
            return false;
        }
        /// <summary>
        /// 尝试直接放置物品
        /// </summary>
        /// <param name="layout"></param>
        /// <param name="item"></param>
        /// <param name="occupied"></param>
        /// <param name="placement"></param>
        /// <returns></returns>
        // 快速路径：只接受完全没有 blocker 的候选位置。
        private static bool TryPlaceDirect(
            InventoryLayout layout,
            SolverItem item,
            Dictionary<RegionCell, int> occupied,
            out Placement placement,
            out List<Placement> candidates)
        {
            candidates = new List<Placement>();
            // 一次扫描完成两件事：缓存几何候选，并寻找完全空闲的位置。
            // 提前成功时缓存可以不完整；只有扫描失败才交给重排使用。
            foreach (Placement candidate in EnumerateCandidates(layout, item))
            {
                candidates.Add(candidate);
                if (CollectBlockers(item, candidate, occupied).Count == 0)
                {
                    placement = candidate;
                    return true;
                }
            }

            placement = default(Placement);
            return false;
        }

        // 只考虑区域边界和有效格，不把可移动物品当成永久障碍。
        private static IEnumerable<Placement> EnumerateCandidates(
            InventoryLayout layout,
            SolverItem item)
        {
            foreach (ShapeVariant variant in item.Rotations.UniqueVariants)
            for (int r = 0; r < layout.Regions.Count; r++)
            {
                RegionLayout region = layout.Regions[r];
                for (int y = 0; y <= region.Height - variant.Height; y++)
                for (int x = 0; x <= region.Width - variant.Width; x++)
                {
                    //遍历所有长宽合法区域能否放置
                    var candidate = new Placement(r, new Cell(x, y), variant.Rotation);
                    bool valid = true;
                    foreach (Cell offset in variant.Cells)
                    {
                        if (!region.IsValid(new Cell(x + offset.X, y + offset.Y)))
                        {
                            valid = false;
                            break;
                        }
                    }
                    if (valid)
                        yield return candidate;
                }
            }
        }

        // 一个物品可能挡住多个格子，但在子问题中只能出现一次。
        private static List<int> CollectBlockers(
            SolverItem item,
            Placement placement,
            Dictionary<RegionCell, int> occupied)
        {
            var blockers = new List<int>();
            foreach (Cell offset in item.Rotations.Get(placement.Rotation).Cells)
            {
                RegionCell key = new RegionCell(
                    placement.RegionIndex,
                    placement.Anchor.X + offset.X,
                    placement.Anchor.Y + offset.Y);
                if (occupied.TryGetValue(key, out int token) && !blockers.Contains(token))
                    blockers.Add(token);
            }
            return blockers;
        }

        // 检查边界、有效格和占用，并收集具体 blocker。
        private static PlacementCheckResult Check(InventoryLayout layout, SolverItem item, Placement placement, Dictionary<RegionCell, int> occupied, out List<int> blockers)
        {
            blockers = new List<int>();
            if (placement.RegionIndex < 0 || placement.RegionIndex >= layout.Regions.Count)
                return new PlacementCheckResult(false, PlacementFailureReason.InvalidRegion);
            RegionLayout region = layout.Regions[placement.RegionIndex];
            foreach (Cell offset in item.Rotations.Get(placement.Rotation).Cells)
            {
                Cell cell = new Cell(placement.Anchor.X + offset.X, placement.Anchor.Y + offset.Y);
                if (cell.X < 0 || cell.Y < 0 || cell.X >= region.Width || cell.Y >= region.Height)
                    return new PlacementCheckResult(false, PlacementFailureReason.OutOfBounds);
                if (!region.IsValid(cell))
                    return new PlacementCheckResult(false, PlacementFailureReason.InvalidCell);
                RegionCell key = Key(placement.RegionIndex, cell);
                if (occupied.TryGetValue(key, out int token) && !blockers.Contains(token))
                    blockers.Add(token);
            }
            return blockers.Count == 0
                ? new PlacementCheckResult(true, PlacementFailureReason.None)
                : new PlacementCheckResult(false, PlacementFailureReason.Occupied);
        }

        // 把当前已有物品转换成求解期间使用的临时占用表。
        private static Dictionary<RegionCell, int> BuildOccupied(InventoryLayout layout, IReadOnlyList<SolverItem> items, int ignoredToken)
        {
            var occupied = new Dictionary<RegionCell, int>();
            foreach (SolverItem item in items)
                if (item.Token != ignoredToken && item.CurrentPlacement.HasValue)
                    AddCells(occupied, item, item.CurrentPlacement.Value);
            return occupied;
        }

        /// <summary>将物品当前形状覆盖的所有格子写入临时占用表。</summary>
        /// <param name="occupied">区域格子到物品 Token 的占用表。</param>
        /// <param name="item">要写入占用表的物品。</param>
        /// <param name="placement">物品在背包中的区域、锚点和旋转。</param>
        private static void AddCells(Dictionary<RegionCell, int> occupied, SolverItem item, Placement placement)
        { foreach (Cell c in item.Rotations.Get(placement.Rotation).Cells) occupied[Key(placement.RegionIndex, new Cell(placement.Anchor.X + c.X, placement.Anchor.Y + c.Y))] = item.Token; }
        /// <summary>从临时占用表移除物品覆盖的所有格子，用于暂时搬移或回滚。</summary>
        /// <param name="occupied">区域格子到物品 Token 的占用表。</param>
        /// <param name="item">要移除的物品。</param>
        /// <param name="placement">物品当前记录的摆放位置。</param>
        private static void RemoveCells(Dictionary<RegionCell, int> occupied, SolverItem item, Placement placement)
        { foreach (Cell c in item.Rotations.Get(placement.Rotation).Cells) occupied.Remove(Key(placement.RegionIndex, new Cell(placement.Anchor.X + c.X, placement.Anchor.Y + c.Y))); }
        /// <summary>将区域索引和局部坐标组合成唯一的占用表键。</summary>
        /// <param name="region">区域索引。</param>
        /// <param name="cell">区域内的局部坐标。</param>
        /// <returns>包含区域信息的完整格子坐标。</returns>
        private static RegionCell Key(int region, Cell cell) { return new RegionCell(region, cell.X, cell.Y); }

        /// <summary>恢复一批临时移除物品的原始位置。</summary>
        /// <param name="occupied">要恢复的临时占用表。</param>
        /// <param name="placements">当前物品位置表。</param>
        /// <param name="items">需要恢复的物品集合。</param>
        private static void Restore(Dictionary<RegionCell, int> occupied, Dictionary<int, Placement> placements, List<SolverItem> items)
        { foreach (SolverItem item in items) { Placement p = item.CurrentPlacement.Value; placements[item.Token] = p; AddCells(occupied, item, p); } }
        /// <summary>将搜索得到的位置表转换为对外返回的求解结果。</summary>
        /// <param name="items">本次求解涉及的物品。</param>
        /// <param name="placements">求解完成后的物品位置表。</param>
        /// <param name="target">目标物品 Token。</param>
        /// <param name="nodes">已访问的搜索节点数量。</param>
        /// <returns>包含最终布局和搜索统计的结果。</returns>
        private static SolveResult BuildResult(Dictionary<int, SolverItem> items, Dictionary<int, Placement> placements, int target, int nodes)
        { var result = new List<SolvedItem>(); foreach (var pair in placements) result.Add(new SolvedItem(pair.Key, pair.Value, true, pair.Key == target)); return new SolveResult(true, result, nodes); }
    }
}
