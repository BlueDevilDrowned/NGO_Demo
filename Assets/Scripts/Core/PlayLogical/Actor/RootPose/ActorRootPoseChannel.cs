using Unity.Netcode;

public sealed class ActorRootPoseChannel
    :ActorSycnChannel<ActorRootPoseSnapshot>
{
    private readonly ActorRootPoseReplication replication;
    private uint lastReceivedServerTick;
    private bool hasReceivedState;

    public override SycnDirection direction=>SycnDirection.ServerToClients;
    public override SyncDataKind DataKind=>SyncDataKind.ContinuousState;
    public override SyncSchedule Schedule=>SyncSchedule.OnChange;

    public ActorRootPoseChannel(
        Actor actor,
        ActorRootPoseReplication replication):base(actor)
    {
        this.replication=replication;
    }

    public override bool TryWrite(uint tick,FastBufferWriter writer)
    {
        if(!actor.IsServer||
           !replication.TryBuildState(out ActorRootPoseSnapshot snapshot))
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

        reader.ReadNetworkSerializable(out ActorRootPoseSnapshot snapshot);
        if(reader.Position!=payloadEnd||
           !ActorRootPoseSnapshotUtility.IsValid(in snapshot))
            return false;

        actor.actorSyncSystem.History.Push(this,tick,in snapshot);
        replication.ReceiveState(in snapshot);
        lastReceivedServerTick=tick;
        hasReceivedState=true;
        return true;
    }
}
