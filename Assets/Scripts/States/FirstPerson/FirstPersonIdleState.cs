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
            stateMachine.ChangeState(target);
    }
}
