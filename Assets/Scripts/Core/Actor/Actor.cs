using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;
[RequireComponent(typeof(CharacterController))]
public partial class Actor : NetworkBehaviour
{
    public Transform player;
    public Transform Cam;
    public CharacterController characterController;
    private NetWorkPlayerController netWorkPlayerController;
    private ActorInputCollector inputCollector;
    public AnimationFacadeBase animationFacadeComponent;
    public IAnimationFacade animationFacade=>animationFacadeComponent;
    public RootMotionDriver motionDriver;
    public ActorMovement movement;
    [Header("配置")]
    public ControllerSO controllerSO;
    public AnimancerData animancerData;
    public AnimationSO animationSO;
    public RunTimeData runTimeData;
    public ActorBrainSo actorBrainSo;
    //
    public StateMachine stateMachine;
    public ActorStateRegistry StateRegistry;
    private ActorStateMachineSynchronizer stateMachineSynchronizer;
    public void Awake()
    {
        //创建rootmotiondriver，movement
        motionDriver=new(this);
        movement=new(this);
        //初始化控制配件
        netWorkPlayerController=new NetWorkPlayerController();
        //1.Facade初始化
        animationFacade.Initialize();
        //2.创建运行时数据
        runTimeData=new();
        //3.创建状态机
        stateMachine=new();
        //4.创建并注册状态
        StateRegistry=new();
        StateRegistry.Initialize(actorBrainSo,this);
        //5.注册完成后启动状态机
        stateMachine.Initialize(StateRegistry.InitialState);
        //更新输入的组件
        inputCollector=new ActorInputCollector(
            netWorkPlayerController,
            runTimeData,
            transform);
        //状态机数据解析组件
        stateMachineSynchronizer=new ActorStateMachineSynchronizer(
            runTimeData,
            stateMachine,
            StateRegistry,
            RefreshMovementIntent);
        //数据同步系统初始化
        InitializeReplication();


    }
    private void Update()
    {
        stateMachineSynchronizer?.ApplyPendingSnapshot();
        stateMachine?.PresentationUpdate(Time.deltaTime);
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
            if (CinemachineCore.VirtualCameraCount > 0 &&
            CinemachineCore.GetVirtualCamera(0) is CinemachineCamera freeLookCamera)
            {
                freeLookCamera.Target.TrackingTarget =
                    player;
                Cam=freeLookCamera.transform;
                freeLookCamera.Target.CustomLookAtTarget = false;
            }
        }
        //

    }
    public override void OnNetworkDespawn()
    {
        NetworkManager.NetworkTickSystem.Tick-=OnNetWorkTick;
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
