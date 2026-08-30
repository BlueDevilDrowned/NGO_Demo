using UnityEngine;

public class ActorMoveStopState : ActorBaseState
{
    private const float ResumeLoopThreshold=0.8f;
    private RootMotionAnimation selectedStop;

    public ActorMoveStopState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        DirectionalLocomotionAnimations transitions=
            GetLocomotionAnimations(actor.simulation.stateData.LastMoveState);
        selectedStop=transitions?.GetStop(
            actor.simulation.stateData.StartFootIsL,
            actor.simulation.stateData.Parameter)??default;
        if(selectedStop.Transition==null)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
            return;
        }

        Play(selectedStop.Transition);
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

    public override void EvaluateMotion()
    {
        TrySubmitRootMotion(selectedStop);
    }
    public override void PresentationUpdate(float deltaTime)
    {
        if(NormalizedTime>=0.5)actor.audioSystem.StopLoop();
    }


    private void OnEndCallback()
    {
        if(stateMachine.CurrentState!=this)return;

        ApplyEndFootPhase(selectedStop.RootData,ref actor.simulation.stateData);
        //进入loop
        stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());

    }
    public override void Exit()
    {
        if(actor.audioSystem.IsLoopPlaying("Walk"))
        {
            actor.audioSystem.StopLoop();
        }
    }

}
