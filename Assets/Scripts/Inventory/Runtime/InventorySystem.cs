using System;
using InventorySolver;

public sealed class InventorySystem : IActorSystem
{
    private readonly Actor actor;
    private readonly InventoryReplication replication;
    private readonly InventoryRuntime runtime;
    private bool isDisposed;

    public InventoryData Data => actor.simulation.inventoryData;
    public InventoryRuntime Runtime => runtime;

    public event Action InventoryChanged;

    public InventorySystem(Actor actor, InventoryLayout layout = null)
    {
        this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
        if (actor.simulation.inventoryData == null)
            actor.simulation.inventoryData = new InventoryData();

        if (layout != null)
        {
            runtime = new InventoryRuntime(layout);
            runtime.Changed += OnRuntimeChanged;
        }
        replication = new InventoryReplication(actor);
        actor.RegisterSystem(this);
    }

    public InventoryRuntime.PlacementPreviewResult PreviewPlacement(
        int instanceId,
        Placement placement)
    {
        if (runtime == null)
            return new InventoryRuntime.PlacementPreviewResult(false, placement, Array.Empty<Cell>());
        return runtime.PreviewPlacement(instanceId, placement);
    }

    public bool TryCommitPlacement(int instanceId, Placement placement)
    {
        if (isDisposed || !actor.IsServer || runtime == null) return false;
        return runtime.TryCommitPlacement(instanceId, placement);
    }

    public bool TryAutoPlace(ItemInstance item, out InventoryRuntime.Entry entry)
    {
        entry = null;
        if (isDisposed || !actor.IsServer || runtime == null) return false;
        return runtime.TryAdd(item, out entry);
    }

    public void PresentationUpdate()
    {
        if (isDisposed || actor.IsServer) return;
        if (replication.TryConsumeState(out _))
            InventoryChanged?.Invoke();
    }

    public void MarkAuthoritativeState()
    {
        if (isDisposed || !actor.IsServer) return;
        InventoryData data = Data;
        replication.MarkAuthoritativeState(
            in data,
            actor.inputSystem.replication.LastReceivedInputTick);
    }

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        if (runtime != null)
            runtime.Changed -= OnRuntimeChanged;
        replication.Dispose();
        InventoryChanged = null;
    }

    private void OnRuntimeChanged()
    {
        if (actor.IsServer)
            SyncRuntimeToData();
        InventoryChanged?.Invoke();
    }

    private void SyncRuntimeToData()
    {
        InventoryData data = new InventoryData();
        foreach (InventoryRuntime.Entry entry in runtime.Entries)
        {
            data.entries.Add(new InventoryData.Entry
            {
                instanceId = entry.InstanceId,
                placement = entry.Placement
            });
        }

        actor.simulation.inventoryData = data;
        MarkAuthoritativeState();
    }
}
