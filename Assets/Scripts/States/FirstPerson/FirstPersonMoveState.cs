public sealed class FirstPersonMoveState : FirstPersonActorState
{
    public FirstPersonMoveState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return IsMoving&&!IsAiming&&
               actor.simulation.locomotionData.stateType!=
                   LocomotionStateType.Sprint&&
               !IsFullBodyState(
                   ActorStateType.Jump,
                   ActorStateType.Fall,
                   ActorStateType.Land);
    }

    public override void Enter()
    {
        PlayMoveLoop();
    }

    public override void PresentationUpdate(float deltaTime)
    {
        LocomotionStateType state=actor.simulation.locomotionData.stateType;
        if(state==presentedState)return;

        PlayMoveLoop();
    }

    public override void ApplyParameter()
    {
        ApplyMoveParameter();
    }

    private LocomotionStateType presentedState;

    private void PlayMoveLoop()
    {
        presentedState=actor.simulation.locomotionData.stateType;
        Play(presentedState==LocomotionStateType.Run
            ?Animations?.Locomotion?.RunLoop??
             Animations?.Locomotion?.WalkLoop??
             Animations?.Idle
            :Animations?.Locomotion?.WalkLoop??
             Animations?.Locomotion?.RunLoop??
             Animations?.Idle);
    }
}
