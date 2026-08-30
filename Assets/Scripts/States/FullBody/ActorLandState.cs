using UnityEngine;

public class ActorLandState : ActorBaseState
{
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

        Animancer.TransitionAsset landing=enteredWithMoveIntent
            ?SelectRunLanding(impactLevel)
            :SelectIdleLanding(impactLevel);

        Play(landing);
        if(landing!=null)
            stateMachine.SetOnEndCallback(OnLandingEnd);
        else
            OnLandingEnd();

        //
        actor.audioSystem.PlayOneShot("Land");
    }
    public override void ServerTick()
    {
        
    }
    public override void EvaluateMotion()
    {
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
        enteredWithMoveIntent=false;
        impactLevel=default;
    }

    private Animancer.TransitionAsset SelectIdleLanding(
        LandingImpactLevel level)
    {
        AirborneFullBodyAnimations landing=Animations?.Airborne;
        return level switch
        {
            LandingImpactLevel.Level4=>landing?.StumbleLand??landing?.HardLand,
            LandingImpactLevel.Level3=>landing?.HardLand??landing?.Land,
            _=>landing?.Land,
        };
    }

    private Animancer.TransitionAsset SelectRunLanding(
        LandingImpactLevel level)
    {
        AirborneFullBodyAnimations landing=Animations?.Airborne;
        return level switch
        {
            LandingImpactLevel.Level4=>landing?.StumbleLand??landing?.HardLand,
            LandingImpactLevel.Level3=>landing?.HardLand??landing?.LandToMove,
            _=>landing?.LandToMove??landing?.Land,
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
