using System;

public sealed class ActorInputReplication : IDisposable
{
    private readonly ActorInputChannel channel;
    private readonly Actor actor;
    private bool isDisposed;
    private bool hasReceivedInput;

    /// <summary>服务器最后接受的输入快照 Tick，用于丢弃重复或乱序输入。</summary>
    public uint LastReceivedInputTick{get;private set;}
    /// <summary>最后接受输入携带的客户端服务器时间估算。</summary>
    public uint LastReceivedServerTick{get;private set;}
    /// <summary>最后接受输入携带的表现 Tick，射击判定使用该时间做历史回溯。</summary>
    public uint LastReceivedPresentedServerTick{get;private set;}
    public bool HasReceivedInput=>hasReceivedInput;

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
        LastReceivedServerTick=snapshot.EstimatedServerTick;
        LastReceivedPresentedServerTick=snapshot.PresentedServerTick;
        hasReceivedInput=true;
        return true;
    }

    public ActorInputData BuildData()
    {
        ActorInputData data=actor.inputSystem.playerController.BuildInputData();
        data.ClientShotId=actor.weapon?.LastLocalShotId??0;
        actor.interactSystem?.PrepareInput(ref data);
        return data;
    }
}
