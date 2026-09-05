using Unity.Netcode;

public sealed class ActorPerspectiveIntentChannel
    : ActorSycnChannel<ActorPerspectiveIntentSnapshot>
{
    private readonly ActorPerspectiveReplication replication;
    private uint lastReceivedIntentTick;
    private bool hasReceivedIntent;
    public override SycnDirection direction=>SycnDirection.OwnerToServer;

    public ActorPerspectiveIntentChannel(
        Actor actor,
        ActorPerspectiveReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override bool TryWrite(uint tick,FastBufferWriter writer)
    {
        if(!replication.TryBuildIntent(
               out ActorPerspectiveIntentSnapshot snapshot))
            return false;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    public override bool TryApply(
        uint tick,
        FastBufferReader reader,
        int payloadEnd)
    {
        if(!actor.IsServer||
           hasReceivedIntent&&tick<=lastReceivedIntentTick)
            return false;

        reader.ReadNetworkSerializable(out ActorPerspectiveIntentSnapshot snapshot);
        if(reader.Position!=payloadEnd||
           !ActorPerspectiveSnapshotUtility.IsValid(snapshot.Mode))
            return false;

        replication.ReceiveIntent(snapshot,tick);
        lastReceivedIntentTick=tick;
        hasReceivedIntent=true;
        return true;
    }
}
