public sealed class UpperBodyWaitState : UpperBodyState
{
    public UpperBodyWaitState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        PlayBasePose(Animations?.Combat?.AimIdle??Animations?.Idle);
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
