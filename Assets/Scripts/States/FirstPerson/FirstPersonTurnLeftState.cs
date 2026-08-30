public sealed class FirstPersonTurnLeftState : FirstPersonActorState
{
    public FirstPersonTurnLeftState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        Play(Animations?.Idle);
    }
}
