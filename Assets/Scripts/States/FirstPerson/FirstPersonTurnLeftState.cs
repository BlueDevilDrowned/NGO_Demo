public sealed class FirstPersonTurnLeftState : FirstPersonActorState
{
    public FirstPersonTurnLeftState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        Play(Animations?.Idle);
    }
    public override void PresentationUpdate(float deltaTime)
    {
        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
        {
            Play(Animations.Combat.Attack);
        }
    }
}
