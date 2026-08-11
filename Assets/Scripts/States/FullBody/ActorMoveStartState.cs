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
        currentState=actor.runTimeData.locomotion.stateType;
        actor.runTimeData.blackboard.LastMoveState=currentState;
        Select();

        actor.runTimeData.blackboard.StartFootIsL=StartFootIsL;
        if(hasSelectedMotion)
            stateMachine.SetOnEndCallback(OnEndCallback);

        //走路音效
        actor.actorAudio.PlayLoop("Walk");
    }
    public override void Exit()
    {
        //走路音效
        actor.actorAudio.StopLoop();
    }


    private void OnEndCallback()
    {
        if(stateMachine.CurrentState!=this)return;

        stateMachine.ChangeState(
            stateRegistry.GetState<ActorMoveLoopState>());
    }

    public override void ServerTick()
    {
        if(actor.runTimeData.WantAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimMoveState>());
            return;
        }

        LocomotionStateType nextState=
            actor.runTimeData.locomotion.stateType;
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
        if(actor.runTimeData.locomotion.DesiredWorldMoveDirection.sqrMagnitude<=0.0001f)
            return;

        LocomotionTransition transitions;
        switch(currentState)
        {
            case LocomotionStateType.Walk:
                transitions=actor.animancerData.Walk;
                break;
            case LocomotionStateType.Jog:
                transitions=actor.animancerData.Jog;
                break;
            default:
                return;
        }

        float angle=actor.runTimeData.locomotion.DesiredLocalMoveAngle;

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
