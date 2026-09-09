using System;

public sealed class InventoryReplication : IActorSystem
{
    private readonly Actor actor;
    private readonly InventoryChannel channel;
    private InventorySnapshot state;
    private bool stateDirty;
    private bool hasPendingState;
    private bool isDisposed;

    public InventoryReplication(Actor actor)
    {
        this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
        channel = new InventoryChannel(actor, this);
        channel.Register();
        stateDirty = actor.IsServer;

        if (actor.simulation.inventoryData == null)
            actor.simulation.inventoryData = new InventoryData();

        if (actor.IsServer)
        {
            InventoryData data = actor.simulation.inventoryData;
            MarkAuthoritativeState(in data, 0);
        }
    }

    public void MarkAuthoritativeState(in InventoryData data, uint tick)
    {
        if (isDisposed || !actor.IsServer) return;
        state = InventorySnapshot.FromData(in data, tick);
        stateDirty = true;
    }

    internal bool TryBuildState(out InventorySnapshot snapshot)
    {
        snapshot = state;
        if (!stateDirty) return false;
        stateDirty = false;
        return true;
    }

    internal void ReceiveState(in InventorySnapshot snapshot)
    {
        state = snapshot;
        actor.simulation.inventoryData = snapshot.ToData();
        hasPendingState = true;
    }

    public bool TryConsumeState(out InventorySnapshot snapshot)
    {
        snapshot = state;
        if (!hasPendingState) return false;
        hasPendingState = false;
        return true;
    }

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        channel.Unregister();
    }
}
