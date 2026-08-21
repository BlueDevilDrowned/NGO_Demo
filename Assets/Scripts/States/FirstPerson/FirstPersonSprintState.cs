public sealed class FirstPersonSprintState : FirstPersonActorState
{
    public FirstPersonSprintState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.PlayTransition(Animations.Sprint,AnimPlayOptions.Default);
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
        float configuredSpeed=actor.actorSO.controllerSO.SprintSpeed;
        float speed=configuredSpeed>0f
            ?configuredSpeed
            :actor.actorSO.controllerSO.JogSpeed;
        SubmitPlanarMovement("FirstPersonSprint",speed);
    }
}
