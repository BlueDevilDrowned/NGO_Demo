public sealed class FirstPersonIdleState : FirstPersonActorState
{
    public FirstPersonIdleState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.PlayTransition(Animations.Idle,AnimPlayOptions.Default);
    }

    public override void ServerTick()
    {
        ActorBaseState target=ResolveGroundedState();
        if(!ReferenceEquals(target,this))
        {
            stateMachine.ChangeState(target);
            return;
        }

        //设置第一人称不旋转最大度数
        if(actor.simulation.CameraBodyYawDelta>actor.actorSO.animationSO.IdleMAxFloat)
        {
            stateMachine.ChangeState(
                stateRegistry.GetState<FirstPersonTurnRightState>());
        }
        else if(actor.simulation.CameraBodyYawDelta<-actor.actorSO.animationSO.IdleMAxFloat)
        {
            stateMachine.ChangeState(
                stateRegistry.GetState<FirstPersonTurnLeftState>());
        }
    }

}
