public sealed class UpperBodyStateSnapshotConsumer
    : IReplicationConsumer<UpperBodyStateSnapshot>
{
    private bool hasReceivedSnapshot;
    private uint lastReceivedTick;
    private bool hasPendingSnapshot;
    private UpperBodyStateSnapshot pendingSnapshot;

    public void Receive(
        in ActorReplicationContext context,
        in UpperBodyStateSnapshot snapshot)
    {
        if(context.IsServer)return;
        if(hasReceivedSnapshot&&snapshot.Tick<=lastReceivedTick)return;

        lastReceivedTick=snapshot.Tick;
        hasReceivedSnapshot=true;
        pendingSnapshot=snapshot;
        hasPendingSnapshot=true;
    }

    public bool TryConsume(out UpperBodyStateSnapshot snapshot)
    {
        snapshot=default;
        if(!hasPendingSnapshot)return false;

        snapshot=pendingSnapshot;
        hasPendingSnapshot=false;
        return true;
    }
}
