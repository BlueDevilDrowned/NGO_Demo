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
        Play(Animations?.Airborne?.FallLoop??
             Animations?.Airborne?.StandingJumpLoop);
    }
    public override void ServerTick()
    {
        GraviteModule gravity=actor.movement.gravite;
        if(!gravity.JustLanded)return;

        stateMachine.ChangeState(stateRegistry.GetState<ActorLandState>());
    }

    public override void EvaluateMotion()
    {
        MovementRequest request=MovementRequest.Default;
        // 身体保持面向逻辑视角，空中位移直接使用相机相对移动方向。
        float inputAmount=Mathf.Clamp01(actor.simulation.inputData.InputMove.magnitude);

        request.WorldPositionDelta=
            actor.simulation.locomotionData.DesiredWorldMoveDirection*
            actor.actorSO.controllerSO.JumpSpeed*
            TickTime.deltaTime*inputAmount;
        request.YawDelta=0f;
        actor.movement.Submit(request);
    }
}
