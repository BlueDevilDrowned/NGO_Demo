using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
[RequireComponent(typeof(CharacterController))]
public partial class Actor : NetworkBehaviour
{
    public Transform player;
    public Transform aimingCore;
    public Transform aimTarget;
    public Transform Muzzle;
    public HitboxManager hitboxManager;
    public ActorAudioEmitter audioEmitter;
    public Rig aimRig;
    public MultiParentConstraint weaponParentConstraint;
    public TwoBoneIKConstraint rightHandIK;
    public TwoBoneIKConstraint leftHandIK;
    public Transform Cam;
    public CharacterController characterController;
    private NetWorkPlayerController netWorkPlayerController;
    private ActorInputCollector inputCollector;
    private ActorInputCommandConsumer inputCommandConsumer;
    private ActorInputSynchronizer inputSynchronizer;
    private LocomotionIntentProcessor locomotionIntentProcessor;
    private LocomotionSnapshotConsumer locomotionSnapshotConsumer;
    private LocomotionSynchronizer locomotionSynchronizer;
    public AnimationFacadeBase animationFacadeComponent;
    public IAnimationFacade animationFacade=>animationFacadeComponent;
    public RootMotionDriver motionDriver;
    public AimSystem aim;
    public ActorMovement movement;
    public WeaponSystem weapon;
    public HealthSystem health;
    public HitReactionSystem hitReaction;
    public ActorAudioSystem actorAudio;
    [Header("配置")]
    public ActorConfig actorConfig;
    public WeaponSO weaponSO;
    public ActorAudioMap audioMap;
    public ControllerSO controllerSO;
    public AimSO aimSO;
    public AnimancerData animancerData;
    public AnimationSO animationSO;
    public RunTimeData runTimeData;
    public ActorBrainSo actorBrainSo;
    //
    public StateMachine stateMachine;
    public UpperBodyStateMachine upperBodyStateMachine;
    public ActorStateRegistry StateRegistry;
    public UpperBodyStateRegistry UpperBodyStateRegistry;
    private ActorGlobalTransitionResolver globalTransitionResolver;
    private ActorStateSnapshotConsumer stateSnapshotConsumer;
    private ActorStateMachineSynchronizer stateMachineSynchronizer;
    private UpperBodyStateSnapshotConsumer upperBodyStateSnapshotConsumer;
    private UpperBodyStateMachineSynchronizer upperBodyStateMachineSynchronizer;
    private WeaponSnapshotConsumer weaponSnapshotConsumer;
    private WeaponSynchronizer weaponSynchronizer;
    private AimSnapshotConsumer aimSnapshotConsumer;
    private AimSynchronizer aimSynchronizer;
    private HealthSnapshotConsumer healthSnapshotConsumer;
    private HealthSynchronizer healthSynchronizer;
    private HitReactionSnapshotConsumer hitReactionSnapshotConsumer;
    private HitReactionSynchronizer hitReactionSynchronizer;

    internal LocalInputData LocalInput=>netWorkPlayerController?.InputData;
    public void Awake()
    {
        if(hitboxManager==null)
            hitboxManager=GetComponentInChildren<HitboxManager>(true);
        hitboxManager?.Initialize(this);
        if(audioEmitter==null)
            audioEmitter=GetComponentInChildren<ActorAudioEmitter>(true);

        //创建rootmotiondriver，movement
        motionDriver=new(this);
        movement=new(this);
        aim=new(this);
        weapon=new(this);
        health=new(this,actorConfig!=null?actorConfig.MaxHealth:100f);
        hitReaction=new(this,animationFacade,animancerData);
        actorAudio=new(audioMap,audioEmitter);
        aim?.SetRigBlendImmediate(0f);
        //初始化控制配件
        netWorkPlayerController=new NetWorkPlayerController();
        //1.Facade初始化//设置上半身层级
        animationFacade.Initialize();
        InitializeAnimationLayers();
        //预先创建动画，防止animancer与animation rigging打架
        PrepareAnimationTransitions();
        //2.创建运行时数据
        runTimeData=new(this);
        inputCommandConsumer=new ActorInputCommandConsumer();
        inputSynchronizer=new ActorInputSynchronizer(
            runTimeData,
            inputCommandConsumer);
        //创建locomotion相关
        locomotionIntentProcessor=new LocomotionIntentProcessor();
        locomotionSnapshotConsumer=new LocomotionSnapshotConsumer();
        locomotionSynchronizer=new LocomotionSynchronizer(
            runTimeData,
            locomotionSnapshotConsumer);
        //3.创建状态机
        stateMachine=new();
        stateMachine.SetStateModeChangedHandler(OnStateModeChanged);

        upperBodyStateMachine=new();
        //4.创建并注册状态
        StateRegistry=new();
        StateRegistry.Initialize(actorBrainSo,this);
        UpperBodyStateRegistry=new();
        UpperBodyStateRegistry.Initialize(actorBrainSo,this);
        //创建全局打断器//状态机
        globalTransitionResolver=new ActorGlobalTransitionResolver(actorBrainSo,StateRegistry);
        //打断判断方法注册进状态机中
        stateMachine.SetGlobalTransitionSelector(globalTransitionResolver.SelectNextState);
        //5.注册完成后启动状态机
        stateMachine.Initialize(StateRegistry.InitialState);
        upperBodyStateMachine.Initialize(UpperBodyStateRegistry.InitialState);
        //更新输入的组件
        inputCollector=new ActorInputCollector(
            netWorkPlayerController,
            runTimeData,
            transform);
        //状态机数据解析组件
        stateSnapshotConsumer=new ActorStateSnapshotConsumer();
        stateMachineSynchronizer=new ActorStateMachineSynchronizer(
            runTimeData,
            stateMachine,
            StateRegistry,
            stateSnapshotConsumer);
        upperBodyStateSnapshotConsumer=new UpperBodyStateSnapshotConsumer();
        upperBodyStateMachineSynchronizer=new UpperBodyStateMachineSynchronizer(
            upperBodyStateMachine,
            UpperBodyStateRegistry,
            upperBodyStateSnapshotConsumer);
        weaponSnapshotConsumer=new WeaponSnapshotConsumer();
        weaponSynchronizer=new WeaponSynchronizer(
            weapon,
            weaponSnapshotConsumer);
        aimSnapshotConsumer=new AimSnapshotConsumer();
        aimSynchronizer=new AimSynchronizer(runTimeData,aimSnapshotConsumer);
        healthSnapshotConsumer=new HealthSnapshotConsumer();
        healthSynchronizer=new HealthSynchronizer(
            health,
            healthSnapshotConsumer);
        hitReactionSnapshotConsumer=new HitReactionSnapshotConsumer();
        hitReactionSynchronizer=new HitReactionSynchronizer(
            hitReaction,
            hitReactionSnapshotConsumer);
        //数据同步系统初始化
        InitializeReplication();
        //aim系统初始化


    }
    private void Update()
    {
        healthSynchronizer?.ApplyPendingSnapshot();
        hitReactionSynchronizer?.ApplyPendingSnapshots();
        locomotionSynchronizer?.ApplyPendingSnapshot();
        aimSynchronizer?.ApplyPendingSnapshot();
        stateMachineSynchronizer?.ApplyPendingSnapshot();
        upperBodyStateMachineSynchronizer?.ApplyPendingSnapshot();
        weaponSynchronizer?.ApplyPendingSnapshot();
        weapon?.PresentationUpdate();
        hitReaction?.PresentationUpdate();
        aim?.PresentationUpdate(Time.deltaTime);
        stateMachine?.PresentationUpdate(Time.deltaTime);
        upperBodyStateMachine?.PresentationUpdate(Time.deltaTime);
    }
    public void SetAimMode(bool isAiming)
    {
        if (!IsOwner || !IsClient || ActorCameraController.Instance == null)
            return;

        ActorCameraController.Instance.SetAimMode(isAiming);
    }
    private void OnStateModeChanged(ActorMode newMode)
    {
        if(newMode==ActorMode.Aiming)
            aim.Active();
        else
            aim.Deactivate();
    }
    private void InitializeAnimationLayers()
    {
        const int upperBodyLayer=1;
        AvatarMask upperBodyMask=animancerData?.UpperBodyMask;
        if(upperBodyMask==null)
        {
            animationFacade.SetLayerWeight(upperBodyLayer,0f);
            Debug.LogError("Upper-body AvatarMask is not configured in AnimancerData.",this);
        }
        else
        {
            animationFacade.SetLayerMask(upperBodyLayer,upperBodyMask);
            animationFacade.SetLayerWeight(upperBodyLayer,0f);
        }

        const int hitReactionLayer=2;
        animationFacade.SetLayerAdditive(hitReactionLayer,true);
        animationFacade.SetLayerWeight(hitReactionLayer,0f);
        AvatarMask hitReactionMask=animancerData?.HitReactionMask;
        if(hitReactionMask!=null)
            animationFacade.SetLayerMask(hitReactionLayer,hitReactionMask);
        else
            Debug.LogWarning(
                "Hit-reaction AvatarMask is not configured; layer 2 will affect every masked transform except root motion.",
                this);
    }
    private void PrepareAnimationTransitions()
    {
        if(animancerData==null)return;

        System.Collections.Generic.IReadOnlyList<AnimationPrewarmEntry> entries=
            animancerData.PrewarmEntries;
        for(int i=0;i<entries.Count;i++)
        {
            AnimationPrewarmEntry entry=entries[i];
            if(entry.Transition!=null)
                animationFacade.PrepareTransition(entry.Transition,entry.Layer);
        }
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if(IsClient)
            weapon.InitializePresentation();
        //加入tick更新
        ActorTickScheduler.Register(this);
        //注册状态机更新
        //注册同步数据
        if(IsOwner)
        {
            //如果是主机，启用控制
            netWorkPlayerController.EnableInput();
            //顺便设置摄像机，以后提供专门入口
            if (IsClient && ActorCameraController.Instance != null)
            {
                ActorCameraController.Instance.Bind(aimingCore,aimSO);
                ActorCameraController.Instance.AimTargetUpdated+=aim.SetOwnerTarget;
                Cam = ActorCameraController.Instance.OutputTransform;
                SetAimMode(stateMachine.CurrentMode==ActorMode.Aiming);
            }
            else if (IsClient)
                Debug.LogError("Local player could not find the scene ActorCameraController.", this);
        }
        //

    }
    public override void OnNetworkDespawn()
    {
        ActorTickScheduler.Unregister(this);
        actorAudio?.StopLoop();
        weapon?.DisposePresentation();
        if (IsOwner && IsClient && ActorCameraController.Instance != null)
        {
            ActorCameraController.Instance.AimTargetUpdated-=aim.SetOwnerTarget;
            ActorCameraController.Instance.Unbind(aimingCore);
        }
        //注销状态机更新

        //断链注销控制
        netWorkPlayerController.DisableInput();
        base.OnNetworkDespawn();
    }
    public override void OnDestroy()
    {
        ActorTickScheduler.Unregister(this);
        actorAudio?.StopLoop();
        weapon?.DisposePresentation();
        snapshotReplicator?.Clear();
        netWorkPlayerController?.Dispose();
        base.OnDestroy();
    }
}
