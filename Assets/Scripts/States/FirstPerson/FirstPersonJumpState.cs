using UnityEngine;

public sealed class FirstPersonJumpState : FirstPersonActorState
{
    private bool hasAppliedJump;

    public FirstPersonJumpState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return actor.simulation.inputData.WasPressed(InputButtons.InputJump)&&
               actor.movement.gravite.IsGrounded;
    }

    public override void Enter()
    {
        hasAppliedJump=false;
        animation.PlayTransition(Animations.JumpUp,AnimPlayOptions.Default);
    }

    public override void Exit()
    {
        hasAppliedJump=false;
    }

    public override void ServerTick()
    {
        if(hasAppliedJump&&actor.movement.gravite.verticalVelocity<0f)
            stateMachine.ChangeState(
                stateRegistry.GetState<FirstPersonFallState>());
    }

    public override void EvaluateMotion()
    {
        MovementRequest request=MovementRequest.Default;
        request.Source="FirstPersonJump";
        if(!hasAppliedJump)
        {
            hasAppliedJump=true;
            request.verticalVelocity.Mode=VerticalVelocityMode.Set;
            request.verticalVelocity.Value=
                actor.actorSO.controllerSO.JumpVelocity;
        }

        request.WorldPositionDelta=
            actor.simulation.locomotionData.DesiredWorldMoveDirection*
            actor.actorSO.controllerSO.JumpSpeed*
            Mathf.Clamp01(actor.simulation.inputData.InputMove.magnitude)*
            TickTime.deltaTime;
        actor.movement.Submit(in request);
    }
}
