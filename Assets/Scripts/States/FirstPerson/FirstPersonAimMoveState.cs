public sealed class FirstPersonAimMoveState : FirstPersonActorState
{
    public FirstPersonAimMoveState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        Play(Animations?.Combat?.AimIdle??Animations?.Idle);
    }

    public override void ApplyParameter()
    {
        ApplyMoveParameter();
    }
    public override void PresentationUpdate(float deltaTime)
    {
        if(!IsAiming)
        {
            TransitionTo(IsMoving
                ?FirstPersonStateType.Move
                :FirstPersonStateType.Idle);
            return;
        }

        if(!IsMoving)
            TransitionTo(FirstPersonStateType.AimIdle);
    }
}
