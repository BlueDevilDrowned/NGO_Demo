using System;

public sealed class UpperBodyStateMachineSynchronizer
{
    private readonly UpperBodyStateMachine stateMachine;
    private readonly UpperBodyStateRegistry stateRegistry;
    private readonly UpperBodyStateSnapshotConsumer consumer;

    public UpperBodyStateMachineSynchronizer(
        UpperBodyStateMachine stateMachine,
        UpperBodyStateRegistry stateRegistry,
        UpperBodyStateSnapshotConsumer consumer)
    {
        this.stateMachine=stateMachine??
            throw new ArgumentNullException(nameof(stateMachine));
        this.stateRegistry=stateRegistry??
            throw new ArgumentNullException(nameof(stateRegistry));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingSnapshot()
    {
        if(!consumer.TryConsume(out UpperBodyStateSnapshot snapshot))return;

        UpperBodyState targetState=stateRegistry.GetState(snapshot.StateType);
        if(targetState==null)return;

        stateMachine.ApplyAuthoritativeState(targetState);
    }
}
