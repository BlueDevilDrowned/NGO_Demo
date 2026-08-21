public sealed class FirstPersonMoveState : FirstPersonActorState
{
    public FirstPersonMoveState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.PlayTransition(Animations.Run,AnimPlayOptions.Default);
        InitializeMixerParameter();
        actor.audioSystem.PlayLoop("Jog");
    }

    public override void Exit()
    {
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
        SubmitPlanarMovement(
            "FirstPersonRun",
            actor.actorSO.controllerSO.JogSpeed);
    }
}
