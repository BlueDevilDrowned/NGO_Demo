public sealed class UpperBodyWaitState : UpperBodyState
{
    public UpperBodyWaitState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.StopLayer(Layer);
        animation.SetLayerWeight(Layer,0f,0.1f);

        //
        
    }

    public override void ServerTick()
    {
        if(!actor.aim.IsActive||
           !actor.runTimeData.Input.IsHeld(InputButtons.InputAttack))return;

        stateMachine.ChangeState(
            stateRegistry.GetState<UpperBodyFireState>());
    }
}
