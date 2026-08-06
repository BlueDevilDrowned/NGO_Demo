using UnityEngine;

public class ActorWalkStartState : ActorBaseState
{
    private bool StartFootIsL;
    private bool hasSelectedMotion;
    private TransitionAndData selectedMotion;

    public ActorWalkStartState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        StartFootIsL=false;
        hasSelectedMotion=false;
        selectedMotion=default;

        Select();

        actor.runTimeData.blackboard.StartFootIsL=StartFootIsL;
        if(hasSelectedMotion)
            stateMachine.SetOnEndCallback(OnEndCallback);
    }

    private void OnEndCallback()
    {
        if(stateMachine.CurrentState!=this)return;

        stateMachine.ChangeState(
            stateRegistry.GetState<ActorWalkLoopState>());
    }

    public override void ServerTick()
    {
        if(actor.runTimeData.WantMove)return;

        if(NormalizedTime>=0.5f)
            stateMachine.ChangeState(
                stateRegistry.GetState<ActorWalkStopState>());
        else
            stateMachine.ChangeState(
                stateRegistry.GetState<ActorIdleState>());
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

        float angle=actor.runTimeData.locomotion.DesiredLocalMoveAngle;

        if(angle>=0f)
        {
            if(angle<22.5f)
                SelectMotion(actor.animancerData.Walk_Start_R0);
            else if(angle<67.5f)
                SelectMotion(actor.animancerData.Walk_Start_R45);
            else if(angle<112.5f)
                SelectMotion(actor.animancerData.Walk_Start_R90);
            else if(angle<157.5f)
                SelectMotion(actor.animancerData.Walk_Start_R135);
            else
                SelectMotion(actor.animancerData.Walk_Start_R180);
        }
        else
        {
            float absoluteAngle=-angle;
            if(absoluteAngle<22.5f)
                SelectMotion(actor.animancerData.Walk_Start_L0);
            else if(absoluteAngle<67.5f)
                SelectMotion(actor.animancerData.Walk_Start_L45);
            else if(absoluteAngle<112.5f)
                SelectMotion(actor.animancerData.Walk_Start_L90);
            else if(absoluteAngle<157.5f)
                SelectMotion(actor.animancerData.Walk_Start_L135);
            else
                SelectMotion(actor.animancerData.Walk_Start_L180);
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
