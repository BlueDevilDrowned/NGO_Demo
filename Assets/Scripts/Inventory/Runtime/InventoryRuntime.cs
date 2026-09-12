using System;
using System.Collections.Generic;
using InventorySolver;

/// <summary>背包业务层雏形：持有物品实例状态，并通过求解器完成放置。</summary>
public sealed class InventoryRuntime
{
    /// <summary>玩家拖动物品时使用的纯查询结果。</summary>
    public sealed class PlacementPreviewResult
    {
        public bool CanPlace { get; }
        public Placement Placement { get; }
        public IReadOnlyList<Cell> OverlappedCells { get; }

        public PlacementPreviewResult(
            bool canPlace,
            Placement placement,
            IReadOnlyList<Cell> overlappedCells)
        {
            CanPlace = canPlace;
            Placement = placement;
            OverlappedCells = overlappedCells;
        }
    }

    /// <summary>背包中的一个物品实例。</summary>
    public sealed class Entry
    {
        public int InstanceId { get; }
        public ItemInstance Item { get; }
        public Placement Placement { get; internal set; }
        internal Entry(int instanceId, ItemInstance item, Placement placement)
        {
            InstanceId = instanceId;
            Item = item;
            Placement = placement;
        }
    }

    private readonly InventoryLayout layout;
    private readonly IInventorySolver solver;
    private readonly List<Entry> entries = new List<Entry>();
    private int nextId = 1;

    public IReadOnlyList<Entry> Entries => entries.AsReadOnly();
    public void Restore(IEnumerable<InventoryData.Entry> source, InventoryItemRegistry registry)
    {
        entries.Clear();
        nextId = 1;
        if (source == null || registry == null) return;
        foreach (var data in source)
        {
            var item = registry.CreateInstance(data.itemId);
            if (item == null) continue;
            entries.Add(new Entry(data.instanceId, item, data.placement));
            if (data.instanceId >= nextId) nextId = data.instanceId + 1;
        }
        Changed?.Invoke();
    }
    public event Action Changed;

    public InventoryRuntime(InventoryLayout layout, IInventorySolver solver = null)
    {
        this.layout = layout ?? throw new ArgumentNullException(nameof(layout));
        this.solver = solver ?? new BacktrackingSolver();
    }

    public bool TryAdd(ItemInstance item, out Entry entry)
    {
        entry = null;
        if (item == null) return false;
        var target = ToSolverItem(item, nextId, null);
        var result = solver.TryAutoPlace(layout, ToSolverItems(), target, new SolverOptions());
        if (!result.Success) return false;
        var solved = Find(result.Items, nextId);
        entry = new Entry(nextId++, item, solved.Placement);
        entries.Add(entry);
        Apply(result);
        Changed?.Invoke();
        return true;
    }

    public PlacementPreviewResult PreviewPlacement(int instanceId, Placement placement)
    {
        Entry entry = entries.Find(x => x.InstanceId == instanceId);
        if (entry == null)
            return new PlacementPreviewResult(false, placement, new List<Cell>());

        PlacementCheckResult check = solver.CanPlace(
            layout,
            ToSolverItems(instanceId),
            ToSolverItem(entry.Item, instanceId, entry.Placement),
            placement);
        return new PlacementPreviewResult(check.CanPlace, placement, new List<Cell>());
    }

    public bool TryCommitPlacement(int instanceId, Placement placement)
    {
        PlacementPreviewResult preview = PreviewPlacement(instanceId, placement);
        if (!preview.CanPlace)
            return false;

        Entry entry = entries.Find(x => x.InstanceId == instanceId);
        entry.Placement = placement;
        Changed?.Invoke();
        return true;
    }

    public bool TryMove(int instanceId, Placement placement)
    {
        return TryCommitPlacement(instanceId, placement);
    }

    public bool Remove(int instanceId)
    {
        int index = entries.FindIndex(x => x.InstanceId == instanceId);
        if (index < 0) return false;
        entries.RemoveAt(index);
        Changed?.Invoke();
        return true;
    }
    private SolverItem ToSolverItem(ItemInstance item, int instanceId, Placement? placement)
    {
        StorageShapeModule module = null;
        if (item.Modules != null)
            foreach (var candidate in item.Modules)
                if (candidate is StorageShapeModule storage) { module = storage; break; }
        var source = module?.Cells;
        var cells = new List<Cell>();
        if (source == null || source.Count == 0) cells.Add(new Cell(0, 0));
        else foreach (var c in source) cells.Add(new Cell(c.x, c.y));
        Shape shape = new Shape(cells);
        return new SolverItem(instanceId, ShapeRotator.Build(shape, true), placement, true);
    }
    private List<SolverItem> ToSolverItems(int ignoredInstanceId = -1)
    {
        List<SolverItem> result = new List<SolverItem>();
        foreach (Entry entry in entries)
        {
            if (entry.InstanceId != ignoredInstanceId)
                result.Add(ToSolverItem(entry.Item, entry.InstanceId, entry.Placement));
        }
        return result;
    }
    private static SolvedItem Find(IReadOnlyList<SolvedItem> solvedItems, int instanceId)
    {
        foreach (SolvedItem solvedItem in solvedItems)
        {
            if (solvedItem.Token == instanceId) return solvedItem;
        }
        throw new InvalidOperationException();
    }
    private void Apply(SolveResult result)
    {
        foreach (SolvedItem solvedItem in result.Items)
        {
            Entry entry = entries.Find(x => x.InstanceId == solvedItem.Token);
            if (entry != null) entry.Placement = solvedItem.Placement;
        }
    }
}



