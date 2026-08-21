public sealed class FirstPersonAimMoveState : FirstPersonActorState
{
    public FirstPersonAimMoveState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        SetAiming(true);
        animation.PlayTransition(Animations.Walk,AnimPlayOptions.Default);
        InitializeMixerParameter();
        actor.audioSystem.PlayLoop("Walk");
    }

    public override void Exit()
    {
        SetAiming(false);
        actor.audioSystem.StopLoop();
    }

    public override void ServerTick()
    {
        ActorBaseState target=ResolveGroundedState();
        if(!ReferenceEquals(target,this))
            stateMachine.ChangeState(target);
    }

    public override void ApplyParameter()
    {
        UpdateMixerParameter();
    }

    public override void EvaluateMotion()
    {
        AimSO config=actor.actorSO.aimSO;
        if(config!=null)
            actor.aimSystem.TrySubmitBodyTurn(
                config.AimMoveYawIgrone,
                config.AimMoveYawMax);

        SubmitPlanarMovement(
            "FirstPersonAimMove",
            actor.actorSO.controllerSO.AimWalkSpeed);
    }
}
