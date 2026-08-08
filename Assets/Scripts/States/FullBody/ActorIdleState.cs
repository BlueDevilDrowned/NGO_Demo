using UnityEngine;

public class ActorIdleState : ActorBaseState
{
    public ActorIdleState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        animation.PlayTransition(actor.animancerData.Idle,AnimPlayOptions.Default);
    }
    public override void ServerTick()
    {
        if(actor.runTimeData.WantAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimIdleState>());
            return;
        }
        if(actor.runTimeData.WantMove)
        {
            //前往start
            stateMachine.ChangeState(stateRegistry.GetState<ActorMoveStartState>());
            return;
        }

    }
}
