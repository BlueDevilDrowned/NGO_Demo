using System;

public sealed class ActorStateSnapshotProducer
    : IReplicationProducer<ActorStateSnapshot>
{
    private readonly RunTimeData runtimeData;
    private readonly StateMachine stateMachine;
    private readonly ActorStateRegistry stateRegistry;

    private bool hasCapturedState;
    private ActorStateType lastCapturedState;
    private uint stateEnterTick;

    public ActorStateSnapshotProducer(
        RunTimeData runtimeData,
        StateMachine stateMachine,
        ActorStateRegistry stateRegistry)
    {
        this.runtimeData=runtimeData??
            throw new ArgumentNullException(nameof(runtimeData));
        this.stateMachine=stateMachine??
            throw new ArgumentNullException(nameof(stateMachine));
        this.stateRegistry=stateRegistry??
            throw new ArgumentNullException(nameof(stateRegistry));
    }

    public bool TryProduce(
        in ActorReplicationContext context,
        out ActorStateSnapshot snapshot)
    {
        snapshot=default;
        if(!context.IsServer)return false;
        if(stateMachine.CurrentState is not ActorBaseState currentState)
            return false;

        ActorStateType currentStateType=stateRegistry.GetStateType(currentState);
        if(!hasCapturedState||currentStateType!=lastCapturedState)
        {
            hasCapturedState=true;
            lastCapturedState=currentStateType;
            stateEnterTick=context.Tick;
        }

        snapshot=new ActorStateSnapshot
        {
            Tick=context.Tick,
            StateType=currentStateType,
            Mode=stateMachine.CurrentMode,
            StateEnterTick=stateEnterTick,
            blackboard=runtimeData.blackboard,
        };
        return true;
    }
}
