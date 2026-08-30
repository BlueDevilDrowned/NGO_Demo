using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(CharacterController))]
public partial class Actor : NetworkBehaviour,IProjectileHitReceiver
{
    [Header("配置文件")]
    [FormerlySerializedAs("weaponRigController")]
    public WeaponRigController weaponRig;
    public ActorSO actorSO;
    [Header("动画输出")]
    public AnimationFacadeBase animationFacadeComponent;
    public AnimationFacadeBase firstPersonAnimationFacadeComponent;
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
    public FirstPersonStateSystem firstPersonStateSystem;
    public InteractSystem interactSystem;

    public IAnimationFacade animationFacade{get;private set;}
    public IAnimationFacade firstPersonAnimationFacade{get;private set;}
    private readonly List<IActorSystem> systems=new();
    private readonly List<IActorOwnershipSystem> ownershipSystems=new();
    private bool isNetworkTickSubscribed;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(actorSO==null)
            throw new InvalidOperationException("Actor requires an ActorSO configuration.");
        if(actorSO.fullBodyAnimation==null)
            Debug.LogError(
                "ActorSO requires a FullBodyAnimationSO configuration.",
                this);
        if(animationFacadeComponent==null)
            throw new InvalidOperationException(
                "Actor requires an explicit full-body animation output.");
        if(IsOwner&&firstPersonAnimationFacadeComponent==null)
            Debug.LogWarning(
                "Owner actor has no first-person animation output configured.",
                this);

        //注意：注册顺序决定了之后生命周期函数的顺序
        actorSyncSystem=new(this);
        simulation=new();
        characterController??=GetComponent<CharacterController>();
        movement=new(this);
        motionDriver=new(this);
        animationArbiter=new(this,animationFacadeComponent);
        animationFacade=animationArbiter;
        animationFacade?.Initialize();
        if(firstPersonAnimationFacadeComponent==animationFacadeComponent)
            throw new InvalidOperationException(
                "Full-body and first-person animation outputs must be different components.");
        firstPersonAnimationFacade=firstPersonAnimationFacadeComponent;
        InitializeAnimationLayers();
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
        actorStateSystem.Initialize(actorSO.actorBrainSO);
        perspectiveSystem=new(this);
        weaponEquipment=new(this,actorSO.WeaponId);
        weapon=new(this,weaponEquipment);
        upperBodyStateSystem=new(this);
        upperBodyStateSystem.Initialize(actorSO.actorBrainSO);
        firstPersonStateSystem=new(this);
        firstPersonStateSystem.Initialize(actorSO.actorBrainSO);
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
        FullBodyAnimationSO animationConfig=actorSO.fullBodyAnimation;

        const int upperBodyLayer=1;
        animationFacade.SetLayerWeight(upperBodyLayer,0f);
        if(animationConfig?.UpperBodyMask!=null)
            animationFacade.SetLayerMask(
                upperBodyLayer,
                animationConfig.UpperBodyMask);
        else
            Debug.LogWarning(
                "Upper-body AvatarMask is not configured in FullBodyAnimationSO.",
                this);

        const int hitReactionLayer=2;
        animationFacade.SetLayerAdditive(hitReactionLayer,true);
        animationFacade.SetLayerWeight(hitReactionLayer,0f);
        if(animationConfig?.HitReactionMask!=null)
            animationFacade.SetLayerMask(
                hitReactionLayer,
                animationConfig.HitReactionMask);
        else
            Debug.LogWarning(
                "Hit-reaction AvatarMask is not configured in FullBodyAnimationSO.",
                this);
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
        firstPersonStateSystem=null;
        perspectiveSystem=null;
        interactSystem=null;
        firstPersonAnimationFacade=null;
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
