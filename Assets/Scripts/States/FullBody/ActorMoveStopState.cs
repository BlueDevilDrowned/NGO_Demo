using UnityEngine;

public class ActorMoveStopState : ActorBaseState
{
    public ActorMoveStopState(Actor actor) : base(actor)
    {
    }
    private RootMotionData data;
    public override void Enter()
    {
        LocomotionTransition transitions=
            actor.runTimeData.blackboard.LastMoveState==LocomotionStateType.Jog
                ?actor.animancerData.Jog
                :actor.animancerData.Walk;

        if(actor.runTimeData.blackboard.StartFootIsL)
        {
            animation.PlayTransition(transitions.Stop_R.transition,AnimPlayOptions.Default);
            data=transitions.Stop_R.data;
        }
        else
        {
            animation.PlayTransition(transitions.Stop_L.transition,AnimPlayOptions.Default);
            data=transitions.Stop_L.data;
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
