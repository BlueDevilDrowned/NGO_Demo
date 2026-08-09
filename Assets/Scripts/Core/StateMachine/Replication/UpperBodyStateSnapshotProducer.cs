using System;

public sealed class UpperBodyStateSnapshotProducer
    : IReplicationProducer<UpperBodyStateSnapshot>
{
    private readonly UpperBodyStateMachine stateMachine;
    private readonly UpperBodyStateRegistry stateRegistry;

    private bool hasCapturedState;
    private UpperBodyStateType lastCapturedState;
    private uint stateEnterTick;

    public UpperBodyStateSnapshotProducer(
        UpperBodyStateMachine stateMachine,
        UpperBodyStateRegistry stateRegistry)
    {
        this.stateMachine=stateMachine??
            throw new ArgumentNullException(nameof(stateMachine));
        this.stateRegistry=stateRegistry??
            throw new ArgumentNullException(nameof(stateRegistry));
    }

    public bool TryProduce(
        in ActorReplicationContext context,
        out UpperBodyStateSnapshot snapshot)
    {
        snapshot=default;
        if(!context.IsServer||stateMachine.CurrentState==null)return false;

        UpperBodyStateType currentStateType=
            stateRegistry.GetStateType(stateMachine.CurrentState);
        if(!hasCapturedState||currentStateType!=lastCapturedState)
        {
            hasCapturedState=true;
            lastCapturedState=currentStateType;
            stateEnterTick=context.Tick;
        }

        snapshot=new UpperBodyStateSnapshot
        {
            Tick=context.Tick,
            StateType=currentStateType,
            StateEnterTick=stateEnterTick,
        };
        return true;
    }
}
