public sealed class FirstPersonLandState : FirstPersonActorState
{
    public FirstPersonLandState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.PlayTransition(Animations.JumpDown,AnimPlayOptions.Default);
        stateMachine.SetOnEndCallback(OnLandingEnd);
        actor.audioSystem.PlayOneShot("Land");
    }

    private void OnLandingEnd()
    {
        if(!ReferenceEquals(stateMachine.CurrentState,this))return;
        stateMachine.ChangeState(ResolveGroundedState());
    }
}
