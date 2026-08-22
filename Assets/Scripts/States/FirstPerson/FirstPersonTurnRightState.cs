public sealed class FirstPersonTurnRightState : FirstPersonActorState
{
    private TransitionAndData TurnMotion =>
        actor.actorSO.animancerData.FirstPerson.TurnRight;

    public FirstPersonTurnRightState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        TransitionAndData motion=TurnMotion;
        if(motion.transition==null)
        {
            stateMachine.ChangeState(
                stateRegistry.GetState<FirstPersonIdleState>());
            return;
        }

        animation.PlayTransition(motion.transition,AnimPlayOptions.Default);
        stateMachine.SetOnEndCallback(OnTurnEnd);
    }

    public override void ServerTick()
    {
        ActorBaseState target=ResolveGroundedState();
        if(!ReferenceEquals(
               target,
               stateRegistry.GetState<FirstPersonIdleState>()))
            stateMachine.ChangeState(target);
    }

    public override void EvaluateMotion()
    {
        RootMotionData data=TurnMotion.data;
        if(data==null||!data.IsBaked)return;

        actor.motionDriver.SubmitClipMotion(data,animation);
    }

    private void OnTurnEnd()
    {
        if(!ReferenceEquals(stateMachine.CurrentState,this))return;

        stateMachine.ChangeState(
            stateRegistry.GetState<FirstPersonIdleState>());
    }
}
