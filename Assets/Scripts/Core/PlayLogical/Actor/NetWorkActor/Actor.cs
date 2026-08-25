using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public partial class Actor : NetworkBehaviour,IProjectileHitReceiver
{
    [Header("配置文件")]
    public AimRigController aimRig;
    [FormerlySerializedAs("weaponRigController")]
    public WeaponRigController weaponRig;
    public ActorSO actorSO;
    public AnimationFacadeBase animationFacadeComponent;
    public Transform player;
    [FormerlySerializedAs("aimingCore")]
    public Transform cameraPivot;
    public Transform firstCameraPivot;
    public CharacterController characterController;
    public HitboxManager hitboxManager;
    public ActorAudioEmitter audioEmitter;
    public ActorViewVisibilityController viewVisibilityController;
    [Header("挂件")]
    public ActorSimulationState simulation;
    public ActorSyncSystem actorSyncSystem;
    public ActorInputSystem inputSystem;
    public ActorCameraSystem cameraSystem;
    public ActorPerspectiveSystem perspectiveSystem;
    public AimSystem aimSystem;
    public LocomotionSystem locomotionSystem;
    public MovementArbiter movement;
    public RootMotionDriver motionDriver;
    public AnimationArbiter animationArbiter;
    public ActorAudioSystem audioSystem;
    public HealthSystem healthSystem;
    public WeaponEquipmentSystem weaponEquipment;
    public WeaponSystem weapon;
    public ActorStateSystem actorStateSystem;
    public UpperBodyStateSystem upperBodyStateSystem;
    public InteractSystem interactSystem;

    public IAnimationFacade animationFacade{get;private set;}
    private readonly List<IActorSystem> systems=new();
    private readonly List<IActorOwnershipSystem> ownershipSystems=new();
    private bool isNetworkTickSubscribed;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(actorSO==null)
            throw new InvalidOperationException("Actor requires an ActorSO configuration.");

        //注意：注册顺序决定了之后生命周期函数的顺序
        actorSyncSystem=new(this);
        simulation=new();
        characterController??=GetComponent<CharacterController>();
        movement=new(this);
        motionDriver=new(this);
        animationFacadeComponent??=GetComponentInChildren<AnimationFacadeBase>(true);
        animationArbiter=new(this,animationFacadeComponent);
        animationFacade=animationArbiter;
        animationFacade?.Initialize();
        InitializeAnimationLayers();
        PrepareAnimationTransitions();
        hitboxManager??=GetComponentInChildren<HitboxManager>(true);
        hitboxManager?.Initialize(this);
        audioEmitter??=GetComponentInChildren<ActorAudioEmitter>(true);
        audioSystem=new(actorSO.audioMap,audioEmitter);
        inputSystem=new(this);
        cameraSystem=new(this);
        aimSystem=new(this);
        locomotionSystem=new(this);
        healthSystem=new(
            this,
            actorSO.actorConfig!=null?actorSO.actorConfig.MaxHealth:100f);
        actorStateSystem=new(this);
        upperBodyStateSystem=new(this);
        actorStateSystem.Initialize(actorSO.actorBrainSO);
        upperBodyStateSystem.Initialize(actorSO.actorBrainSO);
        perspectiveSystem=new(this);
        weaponEquipment=new(this);
        weapon=new(this,weaponEquipment);
        interactSystem=new(this,actorSO.interactSO);

        if(IsOwner)
        {
            for(int i=0;i<ownershipSystems.Count;i++)
                ownershipSystems[i].OnGainedOwnership();
        }

        SubscribeNetworkTick();
    }

    private void InitializeAnimationLayers()
    {
        AnimancerData animancerData=actorSO.animancerData;

        const int upperBodyLayer=1;
        animationFacade.SetLayerWeight(upperBodyLayer,0f);
        if(animancerData.UpperBodyMask!=null)
            animationFacade.SetLayerMask(upperBodyLayer,animancerData.UpperBodyMask);
        else
            Debug.LogError(
                "Upper-body AvatarMask is not configured in AnimancerData.",
                this);

        const int hitReactionLayer=2;
        animationFacade.SetLayerAdditive(hitReactionLayer,true);
        animationFacade.SetLayerWeight(hitReactionLayer,0f);
        if(animancerData.HitReactionMask!=null)
            animationFacade.SetLayerMask(
                hitReactionLayer,
                animancerData.HitReactionMask);
        else
            Debug.LogWarning(
                "Hit-reaction AvatarMask is not configured in AnimancerData.",
                this);
    }

    private void PrepareAnimationTransitions()
    {
        IReadOnlyList<AnimationPrewarmEntry> entries=
            actorSO.animancerData.PrewarmEntries;
        for(int i=0;i<entries.Count;i++)
        {
            AnimationPrewarmEntry entry=entries[i];
            if(entry.Transition!=null)
                animationFacade.PrepareTransition(entry.Transition,entry.Layer);
        }
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
        locomotionSystem=null;
        movement=null;
        motionDriver=null;
        animationArbiter=null;
        audioSystem?.StopLoop();
        audioSystem=null;
        healthSystem=null;
        weaponEquipment=null;
        weapon=null;
        actorStateSystem=null;
        upperBodyStateSystem=null;
        perspectiveSystem=null;
        interactSystem=null;
    }

    public void ReceiveProjectileHit(in ProjectileHitResult hit)
    {
        healthSystem?.ReceiveProjectileHit(in hit);
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
