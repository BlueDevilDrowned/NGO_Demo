public sealed class FirstPersonMoveState : FirstPersonActorState
{
    public FirstPersonMoveState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return IsMoving&&!IsAiming&&
               actor.simulation.locomotionData.stateType!=
                   LocomotionStateType.Jog&&
               !IsFullBodyState(
                   ActorStateType.Jump,
                   ActorStateType.Fall,
                   ActorStateType.Land);
    }

    public override void Enter()
    {
        Play(Animations?.Locomotion?.WalkLoop??
             Animations?.Locomotion?.RunLoop??
             Animations?.Idle);
    }

    public override void ApplyParameter()
    {
        ApplyMoveParameter();
    }
}
