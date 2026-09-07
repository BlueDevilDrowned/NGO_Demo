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
            IEnumerable<Placement> positions = candidates ??
                (IEnumerable<Placement>)EnumerateCandidates(layout, item);
            //对于没有生成后续选列表，则采用迭代器，挨个生成，这样找到合适位置直接返回，不再生成后续候选位置
            foreach (Placement candidate in positions)
            {
                //获得覆盖物品的token
                List<int> blockerTokens = CollectBlockers(item, candidate, occupied);

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
                    Placement old = placements[token];
                    RemoveCells(occupied, blocker, old);
                    placements.Remove(token);
                    removed.Add(blocker); 
                }
                if (blockerTokens.Count != removed.Count)
                    Restore(occupied, placements, removed);

                if (blockerTokens.Count != removed.Count)
                    continue;

                AddCells(occupied, item, candidate);
                placements[item.Token] = candidate;
                bool success = Reinsert(
                    layout, removed, 0, occupied, placements,
                    items, options, depth + 1, ref nodes);
                if (success)
                    return true;

                RemoveCells(occupied, item, candidate);
                placements.Remove(item.Token);
                Restore(occupied, placements, removed);
            }
            return false;
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
            if (SearchTarget(
                    layout, blocker, occupied, placements, items,
                    options, depth, ref nodes))
                return Reinsert(
                    layout, blockers, index + 1, occupied, placements,
                    items, options, depth, ref nodes);
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

        private static void AddCells(Dictionary<RegionCell, int> occupied, SolverItem item, Placement placement)
        { foreach (Cell c in item.Rotations.Get(placement.Rotation).Cells) occupied[Key(placement.RegionIndex, new Cell(placement.Anchor.X + c.X, placement.Anchor.Y + c.Y))] = item.Token; }
        private static void RemoveCells(Dictionary<RegionCell, int> occupied, SolverItem item, Placement placement)
        { foreach (Cell c in item.Rotations.Get(placement.Rotation).Cells) occupied.Remove(Key(placement.RegionIndex, new Cell(placement.Anchor.X + c.X, placement.Anchor.Y + c.Y))); }
        private static RegionCell Key(int region, Cell cell) { return new RegionCell(region, cell.X, cell.Y); }
        private static void Restore(Dictionary<RegionCell, int> occupied, Dictionary<int, Placement> placements, List<SolverItem> items)
        { foreach (SolverItem item in items) { Placement p = item.CurrentPlacement.Value; placements[item.Token] = p; AddCells(occupied, item, p); } }
        private static SolveResult BuildResult(Dictionary<int, SolverItem> items, Dictionary<int, Placement> placements, int target, int nodes)
        { var result = new List<SolvedItem>(); foreach (var pair in placements) result.Add(new SolvedItem(pair.Key, pair.Value, true, pair.Key == target)); return new SolveResult(true, result, nodes); }
    }
}
