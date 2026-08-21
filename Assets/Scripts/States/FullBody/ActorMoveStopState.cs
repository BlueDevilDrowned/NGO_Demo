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
        LocomotionTransition transitions=actor.simulation.stateData.LastMoveState==LocomotionStateType.Jog
                ?actor.actorSO.animancerData.ThirdPerson.Jog
                :actor.actorSO.animancerData.ThirdPerson.Walk;

        if(actor.simulation.stateData.StartFootIsL)
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
        //播放walk音效
        actor.audioSystem.PlayLoop("Walk");
    }

    public override void ServerTick()
    {
        if(actor.simulation.CanAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimIdleState>());
            return;
        }
        if(!actor.simulation.WantMove)return;
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
    public override void PresentationUpdate(float deltaTime)
    {
        if(NormalizedTime>=0.5)actor.audioSystem.StopLoop();
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
        if(actor.audioSystem.IsLoopPlaying("Walk"))
        {
            actor.audioSystem.StopLoop();
        }
    }

    public override void EvaluateMotion()
    {
        if(data==null)return;

        actor.motionDriver.SubmitClipMotion(data,animation);
    }

}
