public sealed class FirstPersonAimIdleState : FirstPersonActorState
{
    public FirstPersonAimIdleState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        SetAiming(true);
        animation.PlayTransition(Animations.AimIdle,AnimPlayOptions.Default);
    }

    public override void Exit()
    {
        SetAiming(false);
    }

    public override void ServerTick()
    {
        ActorBaseState target=ResolveGroundedState();
        if(!ReferenceEquals(target,this))
            stateMachine.ChangeState(target);
    }

    public override void EvaluateMotion()
    {
        AimSO config=actor.actorSO.aimSO;
        if(config!=null)
            actor.aimSystem.TrySubmitBodyTurn(
                config.AimIdleYawIgrone,
                config.AimIdleYawMax);
    }
}
