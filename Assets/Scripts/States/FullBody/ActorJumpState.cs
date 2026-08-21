using UnityEngine;

public class ActorJumpState : ActorBaseState
{
    public ActorJumpState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return actor.simulation.inputData.WasPressed(InputButtons.InputJump)&&
               actor.movement.gravite.IsGrounded;
    }

    bool hasAdd;
    public override void Exit()
    {
        hasAdd=false;
    }
    public override void Enter()
    {
        hasAdd=false;   
        //根据locomotion选择跳跃动画
        LocomotionStateType state=actor.simulation.locomotionData.stateType;
        if(state==LocomotionStateType.Idle)
        {
            animation.PlayTransition(actor.actorSO.animancerData.ThirdPerson.Jump.Idle.Jump_1h.transition,AnimPlayOptions.Default);
        }
        else
        {
            animation.PlayTransition(actor.actorSO.animancerData.ThirdPerson.Jump.RunJump.Jump_1h.transition,AnimPlayOptions.Default);
        }
    }
    public override void EvaluateMotion()
    {
        MovementRequest request=MovementRequest.Default;
        if(!hasAdd)
        {
            hasAdd=true;
            //发出申请
           
            request.Source="JumpForce";
            request.verticalVelocity.Mode=VerticalVelocityMode.Set;
            request.verticalVelocity.Value=actor.actorSO.controllerSO.JumpVelocity;
            
        }
        //水平速度提交//根据摇杆
        float maxYawDelta=actor.actorSO.controllerSO.JumpMaxRotation*TickTime.deltaTime;
        float yawDelta=Mathf.Clamp(
            actor.simulation.locomotionData.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        float inputAmount=Mathf.Clamp01(actor.simulation.inputData.InputMove.magnitude);

        request.ForwardPositionDelta=actor.actorSO.controllerSO.JumpSpeed*TickTime.deltaTime*inputAmount;
        request.YawDelta=yawDelta;
        actor.movement.Submit(request);
    }
    public override void ServerTick()
    {
        if(hasAdd&&NormalizedTime>0.2f&&actor.movement.gravite.verticalVelocity<0)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorFallState>());
        }
    }

}
