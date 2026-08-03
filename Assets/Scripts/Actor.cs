using UnityEngine;

public class Actor : MonoBehaviour
{
    public NetWorkPlayerController netWorkPlayerController;
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
        //1.创建运行时数据
        runTimeData=new();
        //2.创建状态机
        stateMachine=new();
        //3.创建并注册状态
        StateRegistry.Initialize(actorBrainSo,this);
        //4.注册完成后启动状态机
        stateMachine.Initialize(StateRegistry.InitialState);


    }
}
