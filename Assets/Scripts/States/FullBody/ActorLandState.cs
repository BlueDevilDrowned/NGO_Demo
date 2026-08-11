using UnityEngine;

public class ActorLandState : ActorBaseState
{
    private RootMotionData rootMotionData;
    private bool enteredWithMoveIntent;
    private LandingImpactLevel impactLevel;

    public ActorLandState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        impactLevel=actor.controllerSO.GetLandingImpactLevel(
            actor.runTimeData.blackboard.ImpactSpeed);
        enteredWithMoveIntent=
            actor.runTimeData.locomotion.stateType!=LocomotionStateType.Idle;

        TransitionAndData landing=enteredWithMoveIntent
            ?SelectRunLanding(impactLevel)
            :SelectIdleLanding(impactLevel);

        rootMotionData=landing.data;
        animation.PlayTransition(landing.transition,AnimPlayOptions.Default);
        stateMachine.SetOnEndCallback(OnLandingEnd);

        //
        actor.actorAudio.PlayOneShot("Land");
    }
    public override void ServerTick()
    {
        
    }
    public override void EvaluateMotion()
    {
        if(rootMotionData!=null)
            actor.motionDriver.SubmitClipMotion(rootMotionData,animation);

        if(!enteredWithMoveIntent)return;

        float maxYawDelta=
            actor.controllerSO.GetLandingMaxRotation(impactLevel)*
            TickTime.deltaTime;
        MovementRequest request=MovementRequest.Default;
        request.YawDelta=Mathf.Clamp(
            actor.runTimeData.locomotion.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        actor.movement.Submit(request);
    }

    public override void Exit()
    {
        rootMotionData=null;
        enteredWithMoveIntent=false;
        impactLevel=default;
    }

    private TransitionAndData SelectIdleLanding(LandingImpactLevel level)
    {
        LandingTransition landing=actor.animancerData.Landing;
        return level switch
        {
            LandingImpactLevel.Level4=>landing.Land_4h,
            LandingImpactLevel.Level3=>landing.Land_3h,
            LandingImpactLevel.Level2=>landing.Land_2h,
            _=>landing.Land_1h,
        };
    }

    private TransitionAndData SelectRunLanding(LandingImpactLevel level)
    {
        LandingTransition landing=actor.animancerData.Landing;
        return level switch
        {
            LandingImpactLevel.Level4=>landing.Land_ToStumble,
            LandingImpactLevel.Level3=>landing.Land_ToRun3,
            LandingImpactLevel.Level2=>landing.Land_ToRun2,
            _=>landing.Land_ToRun1,
        };
    }

    private void OnLandingEnd()
    {
        if(stateMachine.CurrentState!=this)return;

        if(actor.runTimeData.locomotion.stateType==LocomotionStateType.Idle)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
            return;
        }

        stateMachine.ChangeState(enteredWithMoveIntent
            ?stateRegistry.GetState<ActorMoveLoopState>()
            :stateRegistry.GetState<ActorMoveStartState>());
    }

}
