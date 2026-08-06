using System;

public sealed class LocomotionSnapshotProducer
    : IReplicationProducer<LocomotionSnapshot>
{
    private readonly RunTimeData runtimeData;

    public LocomotionSnapshotProducer(RunTimeData runtimeData)
    {
        this.runtimeData=runtimeData??throw new ArgumentNullException(nameof(runtimeData));
    }

    public bool TryProduce(in ActorReplicationContext context,out LocomotionSnapshot snapshot)
    {
        snapshot=default;
        if(!context.IsServer)return false;

        snapshot=new LocomotionSnapshot
        {
            Tick=context.Tick,
            Data=runtimeData.locomotion,
        };
        return true;
    }
}
