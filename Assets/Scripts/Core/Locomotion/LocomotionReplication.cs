using System;

public sealed class LocomotionReplication : IDisposable
{
    private readonly Actor actor;
    private readonly LocomotionReplicationChannel channel;
    private LocomotionSnapshot state;
    private bool stateDirty;
    private bool hasPendingState;
    private bool isDisposed;

    public LocomotionReplication(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        channel=new LocomotionReplicationChannel(actor,this);
        channel.Register();
        if(actor.IsServer)
            actor.NetworkManager.OnClientConnectedCallback+=OnClientConnected;
    }

    public void MarkAuthoritativeState(in LocomotionData data)
    {
        if(!actor.IsServer)return;

        state=new LocomotionSnapshot{Data=data};
        actor.simulation.locomotionData=data;
        stateDirty=true;
    }

    internal bool TryBuildState(out LocomotionSnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;

        stateDirty=false;
        return true;
    }

    internal void ReceiveState(in LocomotionSnapshot snapshot)
    {
        state=snapshot;
        actor.simulation.locomotionData=snapshot.Data;
        hasPendingState=true;
    }

    public bool TryConsumeState(out LocomotionSnapshot snapshot)
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
