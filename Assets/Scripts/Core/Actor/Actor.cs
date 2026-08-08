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
    public Rig aimRig;
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
    [Header("配置")]
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
    private AimSnapshotConsumer aimSnapshotConsumer;
    private AimSynchronizer aimSynchronizer;

    internal LocalInputData LocalInput=>netWorkPlayerController?.InputData;
    public void Awake()
    {
        //创建rootmotiondriver，movement
        motionDriver=new(this);
        movement=new(this);
        aim=new(this);
        if(aimRig!=null)
            aimRig.weight=0f;
        //初始化控制配件
        netWorkPlayerController=new NetWorkPlayerController();
        //1.Facade初始化
        animationFacade.Initialize();
        //2.创建运行时数据
        runTimeData=new();
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
        aimSnapshotConsumer=new AimSnapshotConsumer();
        aimSynchronizer=new AimSynchronizer(runTimeData,aimSnapshotConsumer);
        //数据同步系统初始化
        InitializeReplication();
        //aim系统初始化
    }
    private void Update()
    {
        locomotionSynchronizer?.ApplyPendingSnapshot();
        aimSynchronizer?.ApplyPendingSnapshot();
        stateMachineSynchronizer?.ApplyPendingSnapshot();
        aim?.PresentationUpdate(Time.deltaTime);
        stateMachine?.PresentationUpdate(Time.deltaTime);
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
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //加入tick更新
        NetworkManager.NetworkTickSystem.Tick+=OnNetWorkTick;
        //注册状态机更新
        //注册同步数据
        if(IsOwner)
        {
            //如果是主机，启用控制
            netWorkPlayerController.EnableInput();
            //顺便设置摄像机，以后提供专门入口
            if (IsClient && ActorCameraController.Instance != null)
            {
                ActorCameraController.Instance.Bind(aimingCore);
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
        NetworkManager.NetworkTickSystem.Tick-=OnNetWorkTick;
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
        snapshotReplicator?.Clear();
        netWorkPlayerController?.Dispose();
        base.OnDestroy();
    }
}
