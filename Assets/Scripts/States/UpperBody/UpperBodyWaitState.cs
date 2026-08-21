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
        if(!actor.simulation.CanAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<UpperBodyEmptyState>());
            return;
        }
        if(!actor.aimSystem.IsAiming||
           !actor.simulation.inputData.IsHeld(InputButtons.InputAttack))return;

        stateMachine.ChangeState(
            stateRegistry.GetState<UpperBodyFireState>());
    }
}
