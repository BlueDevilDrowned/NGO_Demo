using Unity.Netcode;

public sealed class UpperBodyStateReplicationChannel
    : ActorSycnChannel<UpperBodyStateSnapshot>
{
    private readonly UpperBodyStateReplication replication;
    private uint lastReceivedTick;
    private bool hasReceivedState;

    public override SycnDirection direction=>SycnDirection.ServerToClients;

    public UpperBodyStateReplicationChannel(
        Actor actor,
        UpperBodyStateReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override bool TryWrite(uint tick,FastBufferWriter writer)
    {
        if(!replication.TryBuildState(out UpperBodyStateSnapshot snapshot))
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

        reader.ReadNetworkSerializable(out UpperBodyStateSnapshot snapshot);
        if(reader.Position!=payloadEnd)return false;

        replication.ReceiveState(snapshot);
        lastReceivedTick=tick;
        hasReceivedState=true;
        return true;
    }
}
