using Unity.Netcode;
using Unity.VisualScripting;

public sealed class ActorPerspectiveStateChannel
    : ActorSycnChannel<ActorPerspectiveStateSnapshot>
{
    private readonly ActorPerspectiveReplication replication;
    private uint lastReceivedServerTick;
    private bool hasReceivedState;

    
    public override SycnDirection direction=>SycnDirection.ServerToClients;

    public ActorPerspectiveStateChannel(
        Actor actor,
        ActorPerspectiveReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override bool TryWrite(uint tick,FastBufferWriter writer)
    {
        if(!actor.IsServer)return false;
        if(!replication.TryBuildState(
               out ActorPerspectiveStateSnapshot snapshot))
            return false;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    public override bool TryApply(
        uint tick,
        FastBufferReader reader,
        int payloadEnd)
    {
        if(actor.IsServer||hasReceivedState&&tick<=lastReceivedServerTick)
            return false;

        reader.ReadNetworkSerializable(out ActorPerspectiveStateSnapshot snapshot);
        if(reader.Position!=payloadEnd||!ActorPerspectiveSnapshotUtility.IsValid(snapshot.Mode))
            return false;

        replication.ReceiveState(snapshot);
        lastReceivedServerTick=tick;
        hasReceivedState=true;
        return true;
    }
}
