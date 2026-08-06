using System;

public sealed class ActorInputSynchronizer
{
    private readonly RunTimeData runtimeData;
    private readonly ActorInputCommandConsumer consumer;

    public ActorInputSynchronizer(
        RunTimeData runtimeData,
        ActorInputCommandConsumer consumer)
    {
        this.runtimeData=runtimeData??
            throw new ArgumentNullException(nameof(runtimeData));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingCommand()
    {
        if(!consumer.TryConsume(out ActorInputCommand command))return;

        runtimeData.Input=command.Data;
    }
}
