using System;

public sealed class WeaponInventoryReplication : IActorSystem
{
    private readonly Actor actor;
    private readonly WeaponInventoryChannel channel;
    private WeaponInventorySnapshot state;
    private bool stateDirty;
    private bool hasPendingState;
    private bool isDisposed;

    public WeaponInventoryReplication(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        channel=new(actor,this);
        channel.Register();
        stateDirty=actor.IsServer;
        if(actor.IsServer&&actor.NetworkManager!=null)
            actor.NetworkManager.OnClientConnectedCallback+=OnClientConnected;
    }

    public void MarkAuthoritativeState(
        in WeaponInventoryData data,
        uint processedInputTick)
    {
        if(isDisposed||!actor.IsServer)return;

        state=WeaponInventorySnapshot.FromData(
            in data,
            processedInputTick);
        stateDirty=true;
    }

    internal bool TryBuildState(out WeaponInventorySnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;

        stateDirty=false;
        return true;
    }

    internal void ReceiveState(in WeaponInventorySnapshot snapshot)
    {
        state=snapshot;
        actor.simulation.weaponInventoryData=snapshot.ToData();
        hasPendingState=true;
    }

    public bool TryConsumeState(out WeaponInventorySnapshot snapshot)
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

    private void OnClientConnected(ulong _)
    {
        stateDirty=true;
    }
}
