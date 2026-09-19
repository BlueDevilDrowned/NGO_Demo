using Unity.Netcode;

public sealed class InventoryChannel : ActorSycnChannel<InventorySnapshot>
{
    private readonly InventoryReplication replication;
    private bool hasReceivedState;
    private uint lastReceivedTick;

    public InventoryChannel(Actor actor, InventoryReplication replication) : base(actor)
    {
        this.replication = replication;
    }

    public override SycnDirection direction => SycnDirection.ServerToClients;
    public override SyncDataKind DataKind=>SyncDataKind.DiscreteState;
    public override SyncSchedule Schedule=>SyncSchedule.OnChange;

    public override bool TryWrite(uint tick, FastBufferWriter writer)
    {
        if (!replication.TryBuildState(out InventorySnapshot snapshot)) return false;
        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    public override bool TryApply(uint tick, FastBufferReader reader, int payloadEnd)
    {
        if (actor.IsServer || hasReceivedState && tick <= lastReceivedTick) return false;
        reader.ReadNetworkSerializable(out InventorySnapshot snapshot);
        if (reader.Position != payloadEnd || snapshot.EntryCount > InventorySnapshot.MaxEntries) return false;
        replication.ReceiveState(in snapshot);
        hasReceivedState = true;
        lastReceivedTick = tick;
        return true;
    }
}
