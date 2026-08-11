using System.Collections.Generic;
using UnityEngine;

public sealed class HitReactionSnapshotConsumer
    : IReplicationConsumer<HitReactionSnapshot>
{
    private readonly Queue<HitReactionSnapshot> pendingSnapshots=new();
    private bool hasReceivedSnapshot;
    private uint lastReceivedTick;

    public void Receive(
        in ActorReplicationContext context,
        in HitReactionSnapshot snapshot)
    {
        if(context.IsServer)return;
        if(hasReceivedSnapshot&&snapshot.Tick<=lastReceivedTick)return;
        if(snapshot.EventCount>HitReactionSnapshot.MaxEvents)return;
        for(int i=0;i<snapshot.EventCount;i++)
        {
            HitReactionEvent reaction=snapshot.GetEvent(i);
            if(!IsValid(in reaction))return;
        }

        lastReceivedTick=snapshot.Tick;
        hasReceivedSnapshot=true;
        pendingSnapshots.Enqueue(snapshot);
    }

    public bool TryConsume(out HitReactionSnapshot snapshot)
    {
        snapshot=default;
        if(pendingSnapshots.Count==0)return false;

        snapshot=pendingSnapshots.Dequeue();
        return true;
    }

    private static bool IsValid(in HitReactionEvent reaction)
    {
        return (byte)reaction.Location<=(byte)HitLocation.RightFoot&&
               IsFinite(reaction.Direction)&&
               IsFinite(reaction.Damage)&&reaction.Damage>=0f;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x)&&IsFinite(value.y)&&IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
