public sealed class FirstPersonSprintState : FirstPersonActorState
{
    public FirstPersonSprintState(Actor actor) : base(actor)
    {
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

        if(actor.simulation.locomotionData.stateType!=LocomotionStateType.Sprint)
        {
            TransitionTo(FirstPersonStateType.Move);
            return;
        }

        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
        {
            Play(Animations.Combat.Attack);
        }
    }
}
