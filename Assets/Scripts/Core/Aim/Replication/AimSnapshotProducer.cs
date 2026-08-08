using System;

public sealed class AimSnapshotProducer
    : IReplicationProducer<AimSnapshot>
{
    private readonly RunTimeData runtimeData;

    public AimSnapshotProducer(RunTimeData runtimeData)
    {
        this.runtimeData=runtimeData??
            throw new ArgumentNullException(nameof(runtimeData));
    }

    public bool TryProduce(
        in ActorReplicationContext context,
        out AimSnapshot snapshot)
    {
        snapshot=default;
        if(!context.IsServer)return false;

        snapshot=new AimSnapshot
        {
            Tick=context.Tick,
            Data=runtimeData.aim,
        };
        return true;
    }
}
