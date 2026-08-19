using System;

public sealed class HealthReplication : IDisposable
{
    private readonly Actor actor;
    private readonly HealthReplicationChannel channel;
    private HealthSnapshot state;
    private bool stateDirty;
    private bool hasPendingState;
    private bool isDisposed;

    public HealthReplication(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        channel=new HealthReplicationChannel(actor,this);
        channel.Register();
        if(actor.IsServer)
            actor.NetworkManager.OnClientConnectedCallback+=OnClientConnected;
    }

    public void MarkAuthoritativeState(in HealthSnapshot snapshot)
    {
        if(!actor.IsServer)return;

        state=snapshot;
        actor.simulation.currentHealth=snapshot.CurrentHealth;
        actor.simulation.maxHealth=snapshot.MaxHealth;
        stateDirty=true;
    }

    internal bool TryBuildState(out HealthSnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;

        stateDirty=false;
        return true;
    }

    internal void ReceiveState(in HealthSnapshot snapshot)
    {
        state=snapshot;
        actor.simulation.currentHealth=snapshot.CurrentHealth;
        actor.simulation.maxHealth=snapshot.MaxHealth;
        hasPendingState=true;
    }

    public bool TryConsumeState(out HealthSnapshot snapshot)
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
