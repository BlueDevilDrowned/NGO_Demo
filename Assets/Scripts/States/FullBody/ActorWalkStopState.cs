using UnityEngine;

public class ActorWalkStopState : ActorBaseState
{
    public ActorWalkStopState(Actor actor) : base(actor)
    {
    }
    private RootMotionData data;
    public override void Enter()
    {
        
        //先不管左右脚
        if(actor.runTimeData.blackboard.StartFootIsL)
        {
            animation.PlayTransition(actor.animancerData.Walk_Stop_R.transition,AnimPlayOptions.Default);
            data=actor.animancerData.Walk_Stop_R.data;
        }
        else
        {
            animation.PlayTransition(actor.animancerData.Walk_Stop_L.transition,AnimPlayOptions.Default);
            data=actor.animancerData.Walk_Stop_L.data;
        }
        stateMachine.SetOnEndCallback(OnEndCallback);
    }
    private void OnEndCallback()
    {
        if(stateMachine.CurrentState!=this)return;
        //进入loop
        stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());

    }
    public override void Exit()
    {
        data=null;
    }

    public override void EvaluateMotion()
    {
        if(data==null)return;

        actor.motionDriver.SubmitClipMotion(data,animation);
    }

}
