using System;

public sealed class ActorStateReplicationChannel
    : ActorReplicationChannel<ActorStateSnapshot>
{
    public const ushort Id=2;

    private readonly RunTimeData runTimeData;
    private readonly StateMachine stateMachine;
    private readonly ActorStateRegistry stateRegistry;
    private readonly Action refreshMovementIntent;
    private readonly Action<ActorStateType> applyState;

    private bool hasWrittenState;
    private ActorStateType lastWrittenState;
    private uint stateEnterTick;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;

    public ActorStateReplicationChannel(
        RunTimeData runTimeData,
        StateMachine stateMachine,
        ActorStateRegistry stateRegistry,
        Action refreshMovementIntent,
        Action<ActorStateType> applyState)
    {
        // Channel 绑定具体系统及回调，但不直接依赖 Actor，便于独立测试和复用。
        this.runTimeData=runTimeData??
            throw new ArgumentNullException(nameof(runTimeData));
        this.stateMachine=stateMachine??
            throw new ArgumentNullException(nameof(stateMachine));
        this.stateRegistry=stateRegistry??
            throw new ArgumentNullException(nameof(stateRegistry));
        this.refreshMovementIntent=refreshMovementIntent??
            throw new ArgumentNullException(nameof(refreshMovementIntent));
        this.applyState=applyState??
            throw new ArgumentNullException(nameof(applyState));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out ActorStateSnapshot payload)
    {
        payload=default;
        // 状态机快照是服务器权威数据，客户端不能生成下行状态包。
        if(!context.IsServer)return false;
        if(stateMachine.CurrentState is not ActorBaseState currentState)
            return false;

        ActorStateType currentStateType=
            stateRegistry.GetStateType(currentState);

        if(!hasWrittenState||currentStateType!=lastWrittenState)
        {
            // 只有真正切换状态时才更新进入 Tick；字段会随快照发送，
            // 当前 Apply 尚未消费它，后续可用于客户端恢复状态内时间。
            hasWrittenState=true;
            lastWrittenState=currentStateType;
            stateEnterTick=context.Tick;
        }

        payload=new ActorStateSnapshot
        {
            StateType=currentStateType,
            StateEnterTick=stateEnterTick,
            input=runTimeData.Input,
            blackboard=runTimeData.blackboard,
        };
        return true;
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in ActorStateSnapshot payload)
    {
        // Host 同时是 Server 和 Client，但权威数据已经在本地，不应再应用一次。
        if(context.IsServer)return;

        runTimeData.Input=payload.input;
        runTimeData.blackboard=payload.blackboard;
        refreshMovementIntent.Invoke();
        applyState.Invoke(payload.StateType);
    }
}
