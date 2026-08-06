using System;

public sealed class ActorStateReplicationChannel
    : ActorReplicationChannel<ActorStateSnapshot>
{
    public const ushort Id=2;

    private readonly ActorStateMachineSynchronizer synchronizer;

    public override ushort ChannelId=>Id;
    //发送方向
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;

    public ActorStateReplicationChannel(
        ActorStateMachineSynchronizer synchronizer)
    {
        this.synchronizer=synchronizer??
            throw new ArgumentNullException(nameof(synchronizer));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out ActorStateSnapshot payload)
    {
        return synchronizer.TryBuildSnapshot(in context,out payload);
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in ActorStateSnapshot payload)
    {
        synchronizer.ReceiveSnapshot(in context,in payload);
    }
}
