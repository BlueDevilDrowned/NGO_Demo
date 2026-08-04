using Unity.Netcode;
using UnityEngine;

public partial class Actor : NetworkBehaviour
{
    private NetWorkPlayerController netWorkPlayerController;
    public AnimationFacadeBase animationFacadeComponent;
    public IAnimationFacade animationFacade=>animationFacadeComponent;
    
    [Header("配置")]
    public AnimancerData animancerData;
    public RunTimeData runTimeData;
    public ActorBrainSo actorBrainSo;
    //
    public StateMachine stateMachine;
    public ActorStateRegistry StateRegistry;
    public void Awake()
    {
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


    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        //加入tick更新
        NetworkManager.NetworkTickSystem.Tick+=OnNetWorkTick;
        //注册状态机更新
        RegisterNetworkState();

        if(IsOwner)
        {
            //如果是主机，启用控制
            netWorkPlayerController.EnableInput();
        }
    }
    public override void OnNetworkDespawn()
    {
        //注销状态机更新
        UnregisterNetworkState();

        //断链注销控制
        netWorkPlayerController.DisableInput();
        base.OnNetworkDespawn();
    }
    public override void OnDestroy()
    {
        netWorkPlayerController?.Dispose();
        base.OnDestroy();
    }
}
