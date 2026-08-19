using System;

public sealed class ActorStateReplication : IDisposable
{
    private readonly Actor actor;
    private readonly ActorStateReplicationChannel channel;
    private ActorStateSnapshot state;
    private bool stateDirty;
    private bool hasPendingState;
    private bool isDisposed;

    public ActorStateReplication(Actor actor)
    {
        this.actor=actor;
        channel=new ActorStateReplicationChannel(actor,this);
        channel.Register();
        if(actor.IsServer)
            actor.NetworkManager.OnClientConnectedCallback+=OnClientConnected;
    }

    public void MarkAuthoritativeState(in ActorStateSnapshot snapshot)
    {
        if(!actor.IsServer)return;

        state=snapshot;
        actor.simulation.actorState=snapshot;
        stateDirty=true;
    }

    internal bool TryBuildState(out ActorStateSnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;

        stateDirty=false;
        return true;
    }

    internal void ReceiveState(in ActorStateSnapshot snapshot)
    {
        state=snapshot;
        actor.simulation.actorState=snapshot;
        hasPendingState=true;
    }

    public bool TryConsumeState(out ActorStateSnapshot snapshot)
    {
        snapshot=state;
        if(!hasPendingState)return false;

        hasPendingState=false;
        return true;
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        if(actor.NetworkManager!=null)
            actor.NetworkManager.OnClientConnectedCallback-=OnClientConnected;
        channel.Unregister();
    }

    private void OnClientConnected(ulong clientId)
    {
        stateDirty=true;
    }
}
