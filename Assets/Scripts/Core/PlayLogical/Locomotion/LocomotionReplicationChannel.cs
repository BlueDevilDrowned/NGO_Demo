using Unity.Netcode;
using UnityEngine;

public sealed class LocomotionReplicationChannel
    : ActorSycnChannel<LocomotionMotionSnapshot>
{
    private readonly LocomotionReplication replication;
    private bool hasReceivedState;
    private uint lastReceivedTick;

    public LocomotionReplicationChannel(
        Actor actor,
        LocomotionReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override SycnDirection direction=>SycnDirection.ServerToClients;
    public override SyncDataKind DataKind=>SyncDataKind.ContinuousState;
    public override SyncSchedule Schedule=>SyncSchedule.OnChange;

    public override bool TryWrite(uint tick,FastBufferWriter writer)
    {
        if(!actor.IsServer||!replication.TryBuildMotionState(out LocomotionMotionSnapshot snapshot))
            return false;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    public override bool TryApply(
        uint tick,
        FastBufferReader reader,
        int payloadEnd)
    {
        if(actor.IsServer||hasReceivedState&&tick<=lastReceivedTick)
            return false;

        reader.ReadNetworkSerializable(out LocomotionMotionSnapshot snapshot);
        if(reader.Position!=payloadEnd||!IsValid(in snapshot))
            return false;

        actor.actorSyncSystem.History.Push(this,tick,in snapshot);
        replication.ReceiveMotionState(in snapshot);
        hasReceivedState=true;
        lastReceivedTick=tick;
        return true;
    }

    private static bool IsValid(in LocomotionMotionSnapshot data)
    {
        return IsFinite(data.DesiredWorldMoveDirection)&&
               IsFinite(data.DesiredLocalMoveAngle);
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
