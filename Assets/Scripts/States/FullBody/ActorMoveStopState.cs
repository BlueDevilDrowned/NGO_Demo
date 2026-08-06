using UnityEngine;

public class ActorMoveStopState : ActorBaseState
{
    private const float ResumeLoopThreshold=0.8f;

    public ActorMoveStopState(Actor actor) : base(actor)
    {
    }
    private RootMotionData data;
    public override void Enter()
    {
        LocomotionTransition transitions=actor.runTimeData.blackboard.LastMoveState==LocomotionStateType.Jog
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

    public override void ServerTick()
    {
        if(!actor.runTimeData.WantMove)return;
        //期间移动切到loop
        //如果播放进度大于规定值，切到start
        if(NormalizedTime<ResumeLoopThreshold)
        {
            stateMachine.ChangeState(
                stateRegistry.GetState<ActorMoveLoopState>());
            return;
        }

        stateMachine.ChangeState(
            stateRegistry.GetState<ActorMoveStartState>());
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
