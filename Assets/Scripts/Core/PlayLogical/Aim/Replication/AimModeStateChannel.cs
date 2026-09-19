using Unity.Netcode;

public sealed class AimModeStateChannel : ActorSycnChannel<AimModeStateSnapshot>
{
    private readonly AimReplication replication;
    private uint lastReceivedServerTick;
    private bool hasReceivedState;

    public AimModeStateChannel(Actor actor, AimReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override SycnDirection direction=>SycnDirection.ServerToClients;
    public override SyncDataKind DataKind=>SyncDataKind.DiscreteState;
    public override SyncSchedule Schedule=>SyncSchedule.OnChange;

    public override bool TryWrite(uint tick, FastBufferWriter writer)
    {
        if(!actor.IsServer||!replication.TryBuildModeState(out AimModeStateSnapshot snapshot))
            return false;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    public override bool TryApply(uint tick, FastBufferReader reader, int payloadEnd)
    {
        if(actor.IsServer||hasReceivedState&&tick<=lastReceivedServerTick)
            return false;

        reader.ReadNetworkSerializable(out AimModeStateSnapshot snapshot);
        if(reader.Position!=payloadEnd)
            return false;

        replication.ReceiveModeState(in snapshot);
        lastReceivedServerTick=tick;
        hasReceivedState=true;
        return true;
    }
}
