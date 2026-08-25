using Unity.Netcode;
using UnityEngine;

public sealed class LocomotionReplicationChannel
    : ActorSycnChannel<LocomotionSnapshot>
{
    public const ushort Id=9;

    private readonly LocomotionReplication replication;
    private bool hasReceivedState;
    private uint lastReceivedTick;

    public LocomotionReplicationChannel(
        Actor actor,
        LocomotionReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override ushort ChannelId=>Id;
    public override SycnDirection direction=>SycnDirection.ServerToClients;

    public override bool TryWrite(uint tick,FastBufferWriter writer)
    {
        if(!replication.TryBuildState(out LocomotionSnapshot snapshot))
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

        reader.ReadNetworkSerializable(out LocomotionSnapshot snapshot);
        if(reader.Position!=payloadEnd||!IsValid(in snapshot.Data))
            return false;

        replication.ReceiveState(snapshot);
        hasReceivedState=true;
        lastReceivedTick=tick;
        return true;
    }

    private static bool IsValid(in LocomotionData data)
    {
        return IsFinite(data.DesiredWorldMoveDirection)&&
               IsFinite(data.DesiredLocalMoveAngle)&&
               (byte)data.stateType<=(byte)LocomotionStateType.Jog;
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
