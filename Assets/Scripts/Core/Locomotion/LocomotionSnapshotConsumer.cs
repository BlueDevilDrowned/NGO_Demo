using UnityEngine;

public sealed class LocomotionSnapshotConsumer
    : IReplicationConsumer<LocomotionSnapshot>
{
    private bool hasReceivedSnapshot;
    private uint lastReceivedTick;
    private bool hasPendingSnapshot;
    private LocomotionSnapshot pendingSnapshot;

    public void Receive(in ActorReplicationContext context,in LocomotionSnapshot snapshot)
    {
        if(context.IsServer)return;
        if(hasReceivedSnapshot&&snapshot.Tick<=lastReceivedTick)return;
        if(!IsValid(in snapshot.Data))return;

        lastReceivedTick=snapshot.Tick;
        hasReceivedSnapshot=true;
        pendingSnapshot=snapshot;
        hasPendingSnapshot=true;
    }

    public bool TryConsume(out LocomotionSnapshot snapshot)
    {
        snapshot=default;
        if(!hasPendingSnapshot)return false;

        snapshot=pendingSnapshot;
        hasPendingSnapshot=false;
        return true;
    }

    private static bool IsValid(in LocomotionData data)
    {
        return IsFinite(data.DesiredWorldMoveDirection)&&
               !float.IsNaN(data.DesiredLocalMoveAngle)&&
               !float.IsInfinity(data.DesiredLocalMoveAngle);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&
               !float.IsNaN(value.z)&&!float.IsInfinity(value.z);
    }
}
