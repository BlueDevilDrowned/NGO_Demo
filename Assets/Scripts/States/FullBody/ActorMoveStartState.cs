using UnityEngine;

public class ActorMoveStartState : ActorBaseState
{
    private LocomotionStateType currentState;
    private RootMotionAnimation selectedStart;

    public ActorMoveStartState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        currentState=actor.simulation.locomotionData.stateType;
        actor.simulation.stateData.LastMoveState=currentState;
        actor.simulation.stateData.StartFootIsL=false;

        DirectionalLocomotionAnimations transitions=
            GetLocomotionAnimations(currentState);
        selectedStart=transitions?.GetStart(GetLocalMoveDirection())??default;
        if(selectedStart.Transition==null)
        {
            stateMachine.ChangeState(
                stateRegistry.GetState<ActorMoveLoopState>());
            return;
        }

        Play(selectedStart.Transition);
        SetMoveParameter();
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

        ApplyEndFootPhase(selectedStart.RootData,ref actor.simulation.stateData);

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
        if(!TrySubmitRootMotion(selectedStart))
            SubmitMovement();
    }

    public override void ApplyParameter()
    {
        SetMoveParameter();
    }

    private void SetMoveParameter()
    {
        Vector2 localDirection=GetLocalMoveDirection();
        animation.SetMixerParameter(
            localDirection);
    }

    private void SubmitMovement()
    {
        float speed=currentState==LocomotionStateType.Jog
            ?actor.actorSO.controllerSO.JogSpeed
            :actor.actorSO.controllerSO.WalkSpeed;
        float maxYawDelta=(currentState==LocomotionStateType.Jog
            ?actor.actorSO.controllerSO.JogmaxRotation
            :actor.actorSO.controllerSO.WalkmaxRotation)*TickTime.deltaTime;

        MovementRequest request=MovementRequest.Default;
        request.ForwardPositionDelta=
            speed*Mathf.Clamp01(
                actor.simulation.inputData.InputMove.magnitude)*
            TickTime.deltaTime;
        request.YawDelta=Mathf.Clamp(
            actor.simulation.locomotionData.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        request.Source="MoveStart";
        actor.movement.Submit(in request);
    }
}
