using UnityEngine;

public sealed class AimSnapshotConsumer
    : IReplicationConsumer<AimSnapshot>
{
    private bool hasReceivedSnapshot;
    private uint lastReceivedTick;
    private bool hasPendingSnapshot;
    private AimSnapshot pendingSnapshot;

    public void Receive(
        in ActorReplicationContext context,
        in AimSnapshot snapshot)
    {
        if(context.IsServer)return;
        if(hasReceivedSnapshot&&snapshot.Tick<=lastReceivedTick)return;
        if(!IsValid(in snapshot.Data))return;

        lastReceivedTick=snapshot.Tick;
        hasReceivedSnapshot=true;
        pendingSnapshot=snapshot;
        hasPendingSnapshot=true;
    }

    public bool TryConsume(out AimSnapshot snapshot)
    {
        snapshot=default;
        if(!hasPendingSnapshot)return false;

        snapshot=pendingSnapshot;
        hasPendingSnapshot=false;
        return true;
    }

    private static bool IsValid(in AimData data)
    {
        return IsFinite(data.ViewYaw)&&
               IsFinite(data.ViewPitch)&&
               IsFinite(data.TargetPosition);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x)&&IsFinite(value.y)&&IsFinite(value.z);
    }
}
