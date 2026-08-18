using System;

public sealed class ActorInputReplication : IDisposable
{
    private readonly ActorInputChannel channel;
    private readonly Actor actor;
    private bool isDisposed;
    private bool hasReceivedInput;

    public uint LastReceivedInputTick{get;private set;}

    public ActorInputReplication(Actor actor)
    {
        this.actor=actor;
        channel=new(actor);
        channel.Register();
    }

    public void Dispose()
    {
        if(isDisposed)return;

        channel.Unregister();
        isDisposed=true;
    }

    public bool ApplyNetWorkInput(in ActorInputSnapshot snapshot)
    {
        if(hasReceivedInput&&snapshot.Tick<=LastReceivedInputTick)return false;

        actor.simulation.inputData=snapshot.Data;
        LastReceivedInputTick=snapshot.Tick;
        hasReceivedInput=true;
        return true;
    }

    public ActorInputData BuildData()
    {
        ActorInputData data=actor.inputSystem.playerController.BuildInputData();
        return data;
    }
}
