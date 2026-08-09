public sealed class UpperBodyEmptyState : UpperBodyState
{
    public UpperBodyEmptyState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.SetLayerWeight(Layer,0f);
    }

    public override void ServerTick()
    {
        if(!actor.aim.IsActive||
           !actor.runTimeData.Input.IsHeld(InputButtons.InputAttack))return;

        stateMachine.ChangeState(
            stateRegistry.GetState<UpperBodyFireState>());
    }
}
