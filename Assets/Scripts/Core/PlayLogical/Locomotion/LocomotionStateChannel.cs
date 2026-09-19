using Unity.Netcode;

public sealed class LocomotionStateChannel : ActorSycnChannel<LocomotionStateSnapshot>
{
    private readonly LocomotionReplication replication;
    private uint lastReceivedTick;
    private bool hasReceivedState;

    public LocomotionStateChannel(Actor actor, LocomotionReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override SycnDirection direction=>SycnDirection.ServerToClients;
    public override SyncDataKind DataKind=>SyncDataKind.DiscreteState;
    public override SyncSchedule Schedule=>SyncSchedule.OnChange;

    public override bool TryWrite(uint tick, FastBufferWriter writer)
    {
        if(!actor.IsServer||!replication.TryBuildState(out LocomotionStateSnapshot snapshot))
            return false;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    public override bool TryApply(uint tick, FastBufferReader reader, int payloadEnd)
    {
        if(actor.IsServer||hasReceivedState&&tick<=lastReceivedTick)
            return false;

        reader.ReadNetworkSerializable(out LocomotionStateSnapshot snapshot);
        if(reader.Position!=payloadEnd||!IsValid(snapshot.StateType))
            return false;

        replication.ReceiveState(in snapshot);
        lastReceivedTick=tick;
        hasReceivedState=true;
        return true;
    }

    private static bool IsValid(LocomotionStateType value)
    {
        return (byte)value<=(byte)LocomotionStateType.Sprint;
    }
}
