public class ActorAimIdleState : ActorBaseState
{
    public ActorAimIdleState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        if(actor.IsOwner)
            actor.aimSystem.SetPresentationAim(true);

        if(actor.IsServer)
            actor.simulation.aimData.IsAiming=true;
        animation.PlayTransition(actor.actorSO.animancerData.Aiming.Idle,AnimPlayOptions.Default);
    }
    public override void Exit()
    {
        if(actor.IsOwner)
        actor.aimSystem.SetPresentationAim(false);

        if(actor.IsServer)
        actor.simulation.aimData.IsAiming=false;
    }

    public override void ServerTick()
    {
        if(!actor.simulation.CanAim)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
            return;
        }
        if(actor.simulation.WantMove)
        {
            //前往start
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimMoveState>());
            return;
        }
    }
    public override void EvaluateMotion()
    {
        AimSO config=actor.actorSO.aimSO;
        if(config!=null)
            actor.aimSystem.TrySubmitBodyTurn(
                config.AimIdleYawIgrone,
                config.AimIdleYawMax);
    }



}
