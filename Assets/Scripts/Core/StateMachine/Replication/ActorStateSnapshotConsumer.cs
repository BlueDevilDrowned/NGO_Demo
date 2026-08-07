using UnityEngine;

public sealed class ActorStateSnapshotConsumer
    : IReplicationConsumer<ActorStateSnapshot>
{
    private bool hasReceivedSnapshot;
    private uint lastReceivedTick;//是否至少接收过一份合法快照。因为第一次接收lastTick是没用的
    private bool hasPendingSnapshot;
    private ActorStateSnapshot pendingSnapshot;

    public void Receive(in ActorReplicationContext context,in ActorStateSnapshot snapshot)
    {
        if(context.IsServer)return;
        if(hasReceivedSnapshot&&snapshot.Tick<=lastReceivedTick)return;
        if(!IsFinite(snapshot.blackboard.Parameter)||
           !IsFinite(snapshot.blackboard.ImpactSpeed))return;
        //
        lastReceivedTick=snapshot.Tick;
        hasReceivedSnapshot=true;
        //有需要处理的快照
        pendingSnapshot=snapshot;
        hasPendingSnapshot=true;
    }

    public bool TryConsume(out ActorStateSnapshot snapshot)
    {
        snapshot=default;
        if(!hasPendingSnapshot)return false;
        //处理待定的快照
        snapshot=pendingSnapshot;
        hasPendingSnapshot=false;
        return true;
    }

    private static bool IsFinite(Vector2 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
