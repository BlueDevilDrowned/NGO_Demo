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
        impactLevel=actor.actorSO.controllerSO.GetLandingImpactLevel(
            actor.simulation.stateData.ImpactSpeed);
        enteredWithMoveIntent=
            actor.simulation.locomotionData.stateType!=LocomotionStateType.Idle;

        TransitionAndData landing=enteredWithMoveIntent
            ?SelectRunLanding(impactLevel)
            :SelectIdleLanding(impactLevel);

        rootMotionData=landing.data;
        animation.PlayTransition(landing.transition,AnimPlayOptions.Default);
        stateMachine.SetOnEndCallback(OnLandingEnd);

        //
        actor.audioSystem.PlayOneShot("Land");
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
            actor.actorSO.controllerSO.GetLandingMaxRotation(impactLevel)*
            TickTime.deltaTime;
        MovementRequest request=MovementRequest.Default;
        request.YawDelta=Mathf.Clamp(
            actor.simulation.locomotionData.DesiredLocalMoveAngle,
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
        LandingTransition landing=actor.actorSO.animancerData.ThirdPerson.Landing;
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
        LandingTransition landing=actor.actorSO.animancerData.ThirdPerson.Landing;
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

        if(actor.simulation.locomotionData.stateType==LocomotionStateType.Idle)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
            return;
        }

        stateMachine.ChangeState(enteredWithMoveIntent
            ?stateRegistry.GetState<ActorMoveLoopState>()
            :stateRegistry.GetState<ActorMoveStartState>());
    }

}
