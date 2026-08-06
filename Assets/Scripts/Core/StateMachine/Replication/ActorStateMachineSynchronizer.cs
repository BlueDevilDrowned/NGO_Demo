using System;

public sealed class ActorStateMachineSynchronizer
{
    private readonly RunTimeData runtimeData;
    private readonly StateMachine stateMachine;
    private readonly ActorStateRegistry stateRegistry;
    private readonly ActorStateSnapshotConsumer consumer;

    public ActorStateMachineSynchronizer(
        RunTimeData runtimeData,
        StateMachine stateMachine,
        ActorStateRegistry stateRegistry,
        ActorStateSnapshotConsumer consumer)
    {
        this.runtimeData=runtimeData??
            throw new ArgumentNullException(nameof(runtimeData));
        this.stateMachine=stateMachine??
            throw new ArgumentNullException(nameof(stateMachine));
        this.stateRegistry=stateRegistry??
            throw new ArgumentNullException(nameof(stateRegistry));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingSnapshot()
    {
        if(!consumer.TryConsume(out ActorStateSnapshot snapshot))return;

        runtimeData.blackboard=snapshot.blackboard;

        ActorBaseState targetState=stateRegistry.GetState(snapshot.StateType);
        //判断状态是否变化，变化了切换状态来同步到正确的状态
        if(targetState==null||
           ReferenceEquals(stateMachine.CurrentState,targetState))return;

        stateMachine.ChangeState(targetState);
    }
}
