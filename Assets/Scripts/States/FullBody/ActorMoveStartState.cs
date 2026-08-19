using UnityEngine;

public class ActorMoveStartState : ActorBaseState
{
    private bool StartFootIsL;
    private bool hasSelectedMotion;
    private TransitionAndData selectedMotion;
    private LocomotionStateType currentState;

    public ActorMoveStartState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        StartFootIsL=false;
        hasSelectedMotion=false;
        selectedMotion=default;

        //根据locomotion状态决定
        currentState=actor.simulation.locomotionData.stateType;
        actor.simulation.stateData.LastMoveState=currentState;
        Select();

        actor.simulation.stateData.StartFootIsL=StartFootIsL;
        if(hasSelectedMotion)
            stateMachine.SetOnEndCallback(OnEndCallback);

        //走路音效
        actor.audioSystem.PlayLoop("Walk");
    }
    public override void Exit()
    {
        //走路音效
        actor.audioSystem.StopLoop();
    }


    private void OnEndCallback()
    {
        if(stateMachine.CurrentState!=this)return;

        stateMachine.ChangeState(
            stateRegistry.GetState<ActorMoveLoopState>());
    }

    public override void ServerTick()
    {
        if(actor.simulation.CanAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimMoveState>());
            return;
        }

        LocomotionStateType nextState=
            actor.simulation.locomotionData.stateType;
        if(nextState==currentState)return;

        if(nextState==LocomotionStateType.Idle)
        {
            stateMachine.ChangeState(
                stateRegistry.GetState<ActorIdleState>());
            return;
        }

        if(nextState==LocomotionStateType.Walk||
           nextState==LocomotionStateType.Jog)
        {
            stateMachine.ChangeState(
                stateRegistry.GetState<ActorMoveLoopState>());
        }
    }

    public override void EvaluateMotion()
    {
        if(!hasSelectedMotion||selectedMotion.data==null)return;

        actor.motionDriver.SubmitClipMotion(selectedMotion.data,animation);
    }

    private void Select()
    {
        if(actor.simulation.locomotionData.DesiredWorldMoveDirection.sqrMagnitude<=0.0001f)
            return;

        LocomotionTransition transitions;
        switch(currentState)
        {
            case LocomotionStateType.Walk:
                transitions=actor.actorSO.animancerData.Walk;
                break;
            case LocomotionStateType.Jog:
                transitions=actor.actorSO.animancerData.Jog;
                break;
            default:
                return;
        }

        float angle=actor.simulation.locomotionData.DesiredLocalMoveAngle;

        if(angle>=0f)
        {
            if(angle<22.5f)
                SelectMotion(transitions.Start_R0);
            else if(angle<67.5f)
                SelectMotion(transitions.Start_R45);
            else if(angle<112.5f)
                SelectMotion(transitions.Start_R90);
            else if(angle<157.5f)
                SelectMotion(transitions.Start_R135);
            else
                SelectMotion(transitions.Start_R180);
        }
        else
        {
            float absoluteAngle=-angle;
            if(absoluteAngle<22.5f)
                SelectMotion(transitions.Start_L0);
            else if(absoluteAngle<67.5f)
                SelectMotion(transitions.Start_L45);
            else if(absoluteAngle<112.5f)
                SelectMotion(transitions.Start_L90);
            else if(absoluteAngle<157.5f)
                SelectMotion(transitions.Start_L135);
            else
                SelectMotion(transitions.Start_L180);
        }
    }

    private void SelectMotion(TransitionAndData motion)
    {
        if(motion.transition==null)return;

        selectedMotion=motion;
        hasSelectedMotion=true;
        animation.PlayTransition(motion.transition,AnimPlayOptions.Default);

        if(motion.data==null)return;

        if(motion.data.EndFootPhase==BakedFootPhase.LeftFootDown)
            StartFootIsL=false;
        else if(motion.data.EndFootPhase==BakedFootPhase.RightFootDown)
            StartFootIsL=true;
    }
}
