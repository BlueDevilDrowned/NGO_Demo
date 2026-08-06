using System;

public sealed class ActorStateMachineSynchronizer
{
    private readonly RunTimeData runTimeData;
    private readonly StateMachine stateMachine;
    private readonly ActorStateRegistry stateRegistry;
    private readonly Action refreshMovementIntent;

    private bool hasCapturedState;
    private ActorStateType lastCapturedState;
    private uint stateEnterTick;

    private bool hasPendingSnapshot;
    private ActorStateSnapshot pendingSnapshot;

    public ActorStateMachineSynchronizer(
        RunTimeData runTimeData,
        StateMachine stateMachine,
        ActorStateRegistry stateRegistry,
        Action refreshMovementIntent)
    {
        this.runTimeData=runTimeData??
            throw new ArgumentNullException(nameof(runTimeData));
        this.stateMachine=stateMachine??
            throw new ArgumentNullException(nameof(stateMachine));
        this.stateRegistry=stateRegistry??
            throw new ArgumentNullException(nameof(stateRegistry));
        this.refreshMovementIntent=refreshMovementIntent??
            throw new ArgumentNullException(nameof(refreshMovementIntent));
    }

    public bool TryBuildSnapshot(
        in ActorReplicationContext context,
        out ActorStateSnapshot snapshot)
    {
        snapshot=default;
        //状态机只需要主机向客户端发送同步
        if(!context.IsServer)return false;
        if(stateMachine.CurrentState is not ActorBaseState currentState)
            return false;

        ActorStateType currentStateType=
            stateRegistry.GetStateType(currentState);
        //状态发生变化后更新当前状态
        if(!hasCapturedState||currentStateType!=lastCapturedState)
        {
            hasCapturedState=true;
            lastCapturedState=currentStateType;
            stateEnterTick=context.Tick;
        }

        snapshot=new ActorStateSnapshot
        {
            StateType=currentStateType,
            StateEnterTick=stateEnterTick,
            input=runTimeData.Input,
            blackboard=runTimeData.blackboard,
        };
        return true;
    }
    //分为接收和应用两个阶段
    public void ReceiveSnapshot(
        in ActorReplicationContext context,
        in ActorStateSnapshot snapshot)
    {
        // 网络回调只暂存数据，避免在 NGO 的消息处理阶段执行状态 Exit/Enter。
        if(context.IsServer)return;

        pendingSnapshot=snapshot;
        hasPendingSnapshot=true;
    }

    public void ApplyPendingSnapshot()
    {
        if(!hasPendingSnapshot)return;

        ActorStateSnapshot snapshot=pendingSnapshot;
        hasPendingSnapshot=false;

        runTimeData.Input=snapshot.input;
        runTimeData.blackboard=snapshot.blackboard;
        refreshMovementIntent.Invoke();

        ActorBaseState targetState=stateRegistry.GetState(snapshot.StateType);
        if(targetState==null||
           ReferenceEquals(stateMachine.CurrentState,targetState))return;

        stateMachine.ChangeState(targetState);
    }
}
