public sealed class FirstPersonAimMoveState : FirstPersonActorState
{
    public FirstPersonAimMoveState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return IsAiming&&IsMoving&&!IsFullBodyState(
            ActorStateType.Jump,
            ActorStateType.Fall,
            ActorStateType.Land);
    }

    public override void Enter()
    {
        Play(Animations?.Combat?.AimIdle??
             Animations?.Locomotion?.WalkLoop??
             Animations?.Idle);
    }

    public override void ApplyParameter()
    {
        ApplyMoveParameter();
    }
    public override void PresentationUpdate(float deltaTime)
    {
    }
}
