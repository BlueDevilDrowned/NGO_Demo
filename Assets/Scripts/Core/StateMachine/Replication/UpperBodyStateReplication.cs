using System;

public sealed class UpperBodyStateReplication : IDisposable
{
    private readonly Actor actor;
    private readonly UpperBodyStateReplicationChannel channel;
    private UpperBodyStateSnapshot state;
    private bool stateDirty;
    private bool hasPendingState;
    private bool isDisposed;

    public UpperBodyStateReplication(Actor actor)
    {
        this.actor=actor;
        channel=new UpperBodyStateReplicationChannel(actor,this);
        channel.Register();
        if(actor.IsServer)
            actor.NetworkManager.OnClientConnectedCallback+=OnClientConnected;
    }

    public void MarkAuthoritativeState(in UpperBodyStateSnapshot snapshot)
    {
        if(!actor.IsServer)return;

        state=snapshot;
        actor.simulation.upperBodyState=snapshot;
        stateDirty=true;
    }

    internal bool TryBuildState(out UpperBodyStateSnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;

        stateDirty=false;
        return true;
    }

    internal void ReceiveState(in UpperBodyStateSnapshot snapshot)
    {
        state=snapshot;
        actor.simulation.upperBodyState=snapshot;
        hasPendingState=true;
    }

    public bool TryConsumeState(out UpperBodyStateSnapshot snapshot)
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
