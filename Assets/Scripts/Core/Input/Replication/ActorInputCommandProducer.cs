using System;

public sealed class ActorInputCommandProducer
    : IReplicationProducer<ActorInputCommand>
{
    private readonly RunTimeData runtimeData;

    public ActorInputCommandProducer(RunTimeData runtimeData)
    {
        this.runtimeData=runtimeData??
            throw new ArgumentNullException(nameof(runtimeData));
    }

    public bool TryProduce(
        in ActorReplicationContext context,
        out ActorInputCommand command)
    {
        command=default;
        if(!context.IsOwner)return false;

        command=new ActorInputCommand
        {
            Tick=context.Tick,
            Data=runtimeData.Input,
        };
        return true;
    }
}
