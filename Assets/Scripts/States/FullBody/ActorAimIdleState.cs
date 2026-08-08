public class ActorAimIdleState : ActorBaseState
{
    public ActorAimIdleState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        animation.PlayTransition(actor.animancerData.Aiming.Idle,AnimPlayOptions.Default);
    }
    public override void ServerTick()
    {
        if(!actor.runTimeData.WantAim)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
            return;
        }
        if(actor.runTimeData.WantMove)
        {
            //前往start
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimMoveState>());
            return;
        }
    }
    public override void EvaluateMotion()
    {
        actor.aim.TrySubmitBodyTurn(actor.aimSO.AimIdleYawIgrone,actor.aimSO.AimIdleYawMax);
    }



}
