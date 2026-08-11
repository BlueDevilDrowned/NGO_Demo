public sealed class HealthSnapshotConsumer
    : IReplicationConsumer<HealthSnapshot>
{
    private bool hasReceivedSnapshot;
    private uint lastReceivedTick;
    private bool hasPendingSnapshot;
    private HealthSnapshot pendingSnapshot;

    public void Receive(
        in ActorReplicationContext context,
        in HealthSnapshot snapshot)
    {
        if(context.IsServer)return;
        if(hasReceivedSnapshot&&snapshot.Tick<=lastReceivedTick)return;
        if(!IsValid(in snapshot))return;

        lastReceivedTick=snapshot.Tick;
        hasReceivedSnapshot=true;
        pendingSnapshot=snapshot;
        hasPendingSnapshot=true;
    }

    public bool TryConsume(out HealthSnapshot snapshot)
    {
        snapshot=default;
        if(!hasPendingSnapshot)return false;

        snapshot=pendingSnapshot;
        hasPendingSnapshot=false;
        return true;
    }

    private static bool IsValid(in HealthSnapshot snapshot)
    {
        return IsFinite(snapshot.CurrentHealth)&&
               IsFinite(snapshot.MaxHealth)&&
               snapshot.CurrentHealth>=0f&&
               snapshot.MaxHealth>=1f&&
               snapshot.CurrentHealth<=snapshot.MaxHealth;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
