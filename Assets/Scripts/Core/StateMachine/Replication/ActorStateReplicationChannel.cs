using Unity.Netcode;

public sealed class ActorStateReplicationChannel
    : ActorSycnChannel<ActorStateSnapshot>
{
    public const ushort Id=2;

    private readonly ActorStateReplication replication;
    private uint lastReceivedTick;
    private bool hasReceivedState;

    public override ushort ChannelId=>Id;
    public override SycnDirection direction=>SycnDirection.ServerToClients;

    public ActorStateReplicationChannel(
        Actor actor,
        ActorStateReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override bool TryWrite(uint tick,FastBufferWriter writer)
    {
        if(!replication.TryBuildState(out ActorStateSnapshot snapshot))
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

        reader.ReadNetworkSerializable(out ActorStateSnapshot snapshot);
        if(reader.Position!=payloadEnd)return false;

        replication.ReceiveState(snapshot);
        lastReceivedTick=tick;
        hasReceivedState=true;
        return true;
    }
}
