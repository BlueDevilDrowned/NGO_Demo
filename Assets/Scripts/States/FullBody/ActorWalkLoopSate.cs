using UnityEngine;

public class ActorWalkLoopState : ActorBaseState
{
    public ActorWalkLoopState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        if(actor.runTimeData.blackboard.StartFootIsL)animation.PlayTransition(actor.animancerData.Walk_Loop_R,AnimPlayOptions.Default);
        else animation.PlayTransition(actor.animancerData.Walk_Loop_L,AnimPlayOptions.Default);
    }
    public override void ServerTick()
    {
        if(!actor.runTimeData.WantMove)stateMachine.ChangeState(stateRegistry.GetState<ActorWalkStopState>());
    }
}
