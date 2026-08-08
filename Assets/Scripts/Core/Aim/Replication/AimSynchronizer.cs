using System;

public sealed class AimSynchronizer
{
    private readonly RunTimeData runtimeData;
    private readonly AimSnapshotConsumer consumer;

    public AimSynchronizer(
        RunTimeData runtimeData,
        AimSnapshotConsumer consumer)
    {
        this.runtimeData=runtimeData??
            throw new ArgumentNullException(nameof(runtimeData));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingSnapshot()
    {
        if(!consumer.TryConsume(out AimSnapshot snapshot))return;

        runtimeData.aim=snapshot.Data;
    }
}
