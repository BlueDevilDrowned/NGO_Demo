public sealed class UpperBodyEmptyState : UpperBodyState
{
    public UpperBodyEmptyState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        PlayBasePose(Animations?.Idle);
    }
    public override void ServerTick()
    {
        if(actor.simulation.CanAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<UpperBodyWaitState>());
            return;
        }
    }

}
