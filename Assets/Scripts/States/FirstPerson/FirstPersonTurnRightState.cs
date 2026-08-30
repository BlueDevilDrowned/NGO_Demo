public sealed class FirstPersonTurnRightState : FirstPersonActorState
{
    public FirstPersonTurnRightState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        Play(Animations?.Idle);
    }
}
