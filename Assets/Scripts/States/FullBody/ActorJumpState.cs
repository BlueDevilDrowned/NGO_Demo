using UnityEngine;

public class ActorJumpState : ActorBaseState
{
    public ActorJumpState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return actor.runTimeData.Input.WasPressed(InputButtons.InputJump)&&
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
        LocomotionStateType state=actor.runTimeData.locomotion.stateType;
        if(state==LocomotionStateType.Idle)
        {
            animation.PlayTransition(actor.animancerData.Jump.Idle.Jump_1h.transition,AnimPlayOptions.Default);
        }
        else
        {
            animation.PlayTransition(actor.animancerData.Jump.RunJump.Jump_1h.transition,AnimPlayOptions.Default);
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
            request.verticalVelocity.Value=actor.controllerSO.JumpVelocity;
            
        }
        //水平速度提交//根据摇杆
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
    public override void ServerTick()
    {
        if(hasAdd&&NormalizedTime>0.2f&&actor.movement.gravite.verticalVelocity<0)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorFallState>());
        }
    }

}
