using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public partial class Actor : NetworkBehaviour
{
    public ActorSimulationState simulation;
    public ActorSyncSystem actorSyncSystem;
    public ActorInputSystem inputSystem;

    private readonly List<IActorSystem> systems=new();
    private readonly List<IActorOwnershipSystem> ownershipSystems=new();
    private bool isNetworkTickSubscribed;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //注意：注册顺序决定了之后生命周期函数的顺序
        actorSyncSystem=new(this);
        simulation=new();
        inputSystem=new(this);

        SubscribeNetworkTick();
    }

    internal void RegisterSystem(IActorSystem system)
    {
        if(system==null)throw new ArgumentNullException(nameof(system));
        if(systems.Contains(system))
            throw new InvalidOperationException(
                $"Actor system {system.GetType().Name} is already registered.");

        systems.Add(system);
        if(system is IActorOwnershipSystem ownershipSystem)
            ownershipSystems.Add(ownershipSystem);
    }

    public override void OnGainedOwnership()
    {
        base.OnGainedOwnership();

        for(int i=0;i<ownershipSystems.Count;i++)
            ownershipSystems[i].OnGainedOwnership();
    }

    public override void OnLostOwnership()
    {
        for(int i=ownershipSystems.Count-1;i>=0;i--)
            ownershipSystems[i].OnLostOwnership();

        base.OnLostOwnership();
    }

    public override void OnNetworkDespawn()
    {
        UnsubscribeNetworkTick();
        DisposeSystems();

        base.OnNetworkDespawn();
    }

    public override void OnDestroy()
    {
        UnsubscribeNetworkTick();
        DisposeSystems();

        base.OnDestroy();
    }

    private void DisposeSystems()
    {
        for(int i=systems.Count-1;i>=0;i--)
            systems[i].Dispose();

        ownershipSystems.Clear();
        systems.Clear();

        inputSystem=null;
        actorSyncSystem=null;
        simulation=null;
    }

    private void SubscribeNetworkTick()
    {
        if(isNetworkTickSubscribed)return;

        NetworkManager.NetworkTickSystem.Tick+=Tick;
        isNetworkTickSubscribed=true;
    }

    private void UnsubscribeNetworkTick()
    {
        if(!isNetworkTickSubscribed)return;

        if(NetworkManager!=null)
            NetworkManager.NetworkTickSystem.Tick-=Tick;

        isNetworkTickSubscribed=false;
    }
}
