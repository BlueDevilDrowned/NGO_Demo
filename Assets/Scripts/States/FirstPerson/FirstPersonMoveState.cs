public sealed class FirstPersonMoveState : FirstPersonActorState
{
    public FirstPersonMoveState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        PlayMoveLoop();
    }

    public override void PresentationUpdate(float deltaTime)
    {
        if(IsAiming)
        {
            TransitionTo(FirstPersonStateType.AimMove);
            return;
        }

        if(!IsMoving)
        {
            TransitionTo(FirstPersonStateType.Idle);
            return;
        }

        if(actor.simulation.locomotionData.stateType==LocomotionStateType.Sprint)
        {
            TransitionTo(FirstPersonStateType.Sprint);
            return;
        }

        LocomotionStateType state=actor.simulation.locomotionData.stateType;
        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
        {
            Play(Animations.Combat.Attack);
            return;
        }

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
