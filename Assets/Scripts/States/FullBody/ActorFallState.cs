using UnityEngine;

public class ActorFallState : ActorBaseState
{
    public ActorFallState(Actor actor) : base(actor)
    {
    }
    public override bool CanEnterFrom(BaseState currentState)
    {
        return !actor.movement.gravite.IsGrounded;
    }

    public override void Enter()
    {
        //
        animation.PlayTransition(actor.animancerData.Fall,AnimPlayOptions.Default);
    }
    public override void ServerTick()
    {
        GraviteModule gravity=actor.movement.gravite;
        if(!gravity.JustLanded)return;

        actor.runTimeData.blackboard.ImpactSpeed=gravity.LastImpactSpeed;
        stateMachine.ChangeState(stateRegistry.GetState<ActorLandState>());
    }

    public override void EvaluateMotion()
    {
        MovementRequest request=MovementRequest.Default;
        //水平速度提交//根据摇杆//与jump一致
        float maxYawDelta=actor.controllerSO.JumpMaxRotation*TickTime.deltaTime;
        float yawDelta=Mathf.Clamp(
            actor.runTimeData.locomotion.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        float inputAmount=Mathf.Clamp01(actor.runTimeData.Input.InputMove.magnitude);

        request.ForwardPositionDelta=actor.controllerSO.JumpSpeed*TickTime.deltaTime*inputAmount;
        request.YawDelta=yawDelta;
        actor.movement.Submit(request);
    }
}
