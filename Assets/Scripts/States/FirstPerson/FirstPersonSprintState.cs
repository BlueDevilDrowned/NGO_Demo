public sealed class FirstPersonSprintState : FirstPersonActorState
{
    public FirstPersonSprintState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return IsMoving&&!IsAiming&&
               actor.simulation.locomotionData.stateType==
                   LocomotionStateType.Sprint&&
               !IsFullBodyState(
                   ActorStateType.Jump,
                   ActorStateType.Fall,
                   ActorStateType.Land);
    }

    public override void Enter()
    {
        Play(Animations?.Locomotion?.SprintLoop??
             Animations?.Locomotion?.RunLoop??
             Animations?.Locomotion?.WalkLoop??
             Animations?.Idle);
    }

    public override void ApplyParameter()
    {
        ApplyMoveParameter();
    }
}
