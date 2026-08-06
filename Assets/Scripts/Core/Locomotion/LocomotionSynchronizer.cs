using System;

public sealed class LocomotionSynchronizer
{
    private readonly RunTimeData runtimeData;
    private readonly LocomotionSnapshotConsumer consumer;

    public LocomotionSynchronizer(RunTimeData runtimeData,LocomotionSnapshotConsumer consumer)
    {
        this.runtimeData=runtimeData??
            throw new ArgumentNullException(nameof(runtimeData));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingSnapshot()
    {
        if(!consumer.TryConsume(out LocomotionSnapshot snapshot))return;

        runtimeData.locomotion=snapshot.Data;
    }
}
