public sealed class FirstPersonAimIdleState : FirstPersonActorState
{
    public FirstPersonAimIdleState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return IsAiming&&!IsMoving&&!IsFullBodyState(
            ActorStateType.Jump,
            ActorStateType.Fall,
            ActorStateType.Land);
    }

    public override void Enter()
    {
        Play(Animations?.Combat?.AimIdle??Animations?.Idle);
    }
}
