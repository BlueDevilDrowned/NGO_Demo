using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponSnapshotConsumer
    : IReplicationConsumer<WeaponSnapshot>
{
    private readonly Queue<WeaponSnapshot> pendingSnapshots=new();
    private bool hasReceivedSnapshot;
    private uint lastReceivedTick;

    public void Receive(
        in ActorReplicationContext context,
        in WeaponSnapshot snapshot)
    {
        if(context.IsServer)return;
        if(hasReceivedSnapshot&&snapshot.Tick<=lastReceivedTick)return;
        if(snapshot.EventCount>WeaponSnapshot.MaxEvents)return;
        for(int i=0;i<snapshot.EventCount;i++)
        {
            ShotData shotEvent=snapshot.GetEvent(i);
            if(!IsValid(in shotEvent))return;
        }

        lastReceivedTick=snapshot.Tick;
        hasReceivedSnapshot=true;
        pendingSnapshots.Enqueue(snapshot);
    }

    public bool TryConsume(out WeaponSnapshot snapshot)
    {
        snapshot=default;
        if(pendingSnapshots.Count==0)return false;

        snapshot=pendingSnapshots.Dequeue();
        return true;
    }

    private static bool IsValid(in ShotData shot)
    {
        return shot.EventType<=ShotEventType.Expired&&
               (shot.HitLayer<=31||shot.HitLayer==byte.MaxValue)&&
               IsFinite(shot.TracerSpeed)&&shot.TracerSpeed>=0f&&
               IsFinite(shot.Gravity)&&shot.Gravity>=0f&&
               IsFinite(shot.Range)&&shot.Range>=0f&&
               IsFinite(shot.Origin)&&IsFinite(shot.EndPoint)&&
               IsFinite(shot.HitNormal);
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
