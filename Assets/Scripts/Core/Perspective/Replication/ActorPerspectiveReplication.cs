public sealed class ActorPerspectiveReplication : IActorSystem
{
    private readonly Actor actor;
    private readonly ActorPerspectiveIntentChannel intentChannel;
    private readonly ActorPerspectiveStateChannel stateChannel;

    private ActorPerspectiveIntentSnapshot outgoingIntent;
    private bool intentDirty;
    private ActorPerspectiveRequest pendingIntent;
    private bool hasPendingIntent;
    private ActorPerspectiveStateSnapshot state;
    private bool stateDirty;
    private bool hasReceivedState;

    private bool isDisposed;

    public ActorPerspectiveReplication(
        Actor actor,
        CameraPerspectiveMode initialMode)
    {
        this.actor=actor;
        state=new ActorPerspectiveStateSnapshot{Mode=initialMode};
        stateDirty=actor.IsServer;

        intentChannel=new ActorPerspectiveIntentChannel(actor,this);
        intentChannel.Register();

        stateChannel=new ActorPerspectiveStateChannel(actor,this);
        stateChannel.Register();

        if(actor.IsServer)
            actor.NetworkManager.OnClientConnectedCallback+=OnClientConnected;
    }

    public void SubmitIntent(CameraPerspectiveMode mode)
    {
        if(!actor.IsOwner)return;

        outgoingIntent=new ActorPerspectiveIntentSnapshot{Mode=mode};
        intentDirty=true;
    }

    internal bool TryBuildIntent(out ActorPerspectiveIntentSnapshot snapshot)
    {
        snapshot=outgoingIntent;
        if(!intentDirty)return false;

        intentDirty=false;
        return true;
    }

    internal void ReceiveIntent(
        in ActorPerspectiveIntentSnapshot snapshot,
        uint inputTick)
    {
        pendingIntent=new ActorPerspectiveRequest
        {
            Mode=snapshot.Mode,
            InputTick=inputTick,
        };
        hasPendingIntent=true;
    }

    public bool TryConsumeIntent(out ActorPerspectiveRequest request)
    {
        request=pendingIntent;
        if(!hasPendingIntent)return false;

        hasPendingIntent=false;
        return true;
    }

    public void MarkAuthoritativeState(
        CameraPerspectiveMode mode,
        uint processedInputTick)
    {
        if(!actor.IsServer)return;

        state=new ActorPerspectiveStateSnapshot
        {
            Mode=mode,
            ProcessedInputTick=processedInputTick,
        };
        actor.simulation.perspectiveMode=mode;
        stateDirty=true;
    }

    internal bool TryBuildState(out ActorPerspectiveStateSnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;

        stateDirty=false;
        return true;
    }

    internal void ReceiveState(in ActorPerspectiveStateSnapshot snapshot)
    {
        state=snapshot;
        actor.simulation.perspectiveMode=snapshot.Mode;
        hasReceivedState=true;
    }

    public bool TryConsumeState(out ActorPerspectiveStateSnapshot snapshot)
    {
        snapshot=state;
        if(!hasReceivedState)return false;

        hasReceivedState=false;
        return true;
    }

    private void OnClientConnected(ulong clientId)
    {
        stateDirty=true;
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        if(actor.NetworkManager!=null)
            actor.NetworkManager.OnClientConnectedCallback-=OnClientConnected;
        intentChannel.Unregister();
        stateChannel.Unregister();
    }
}
