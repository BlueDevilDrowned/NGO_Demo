using UnityEngine;

public class ActorJumpState : ActorBaseState
{
    private bool enteredWithMoveIntent;
    private bool hasAdd;

    public ActorJumpState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return actor.simulation.inputData.WasPressed(InputButtons.InputJump)&&
               actor.movement.gravite.IsGrounded;
    }

    public override void Exit()
    {
        hasAdd=false;
        enteredWithMoveIntent=false;
    }

    public override void Enter()
    {
        hasAdd=false;
        enteredWithMoveIntent=
            actor.simulation.locomotionData.stateType!=LocomotionStateType.Idle;

        AirborneFullBodyAnimations airborne=Animations?.Airborne;
        Animancer.TransitionAsset start=enteredWithMoveIntent
            ?airborne?.MovingJumpStart
            :airborne?.StandingJumpStart;

        if(start==null)
        {
            PlayJumpLoop();
            return;
        }

        Play(start);
        stateMachine.SetOnEndCallback(PlayJumpLoop);
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
        // 身体保持面向逻辑视角，空中位移直接使用相机相对移动方向。
        float inputAmount=Mathf.Clamp01(actor.simulation.inputData.InputMove.magnitude);

        request.WorldPositionDelta=
            actor.simulation.locomotionData.DesiredWorldMoveDirection*
            actor.actorSO.controllerSO.JumpSpeed*
            TickTime.deltaTime*inputAmount;
        request.YawDelta=0f;
        actor.movement.Submit(request);
    }
    public override void ServerTick()
    {
        if(hasAdd&&actor.movement.gravite.verticalVelocity<0)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorFallState>());
        }
    }

    private void PlayJumpLoop()
    {
        if(stateMachine.CurrentState!=this)return;

        AirborneFullBodyAnimations airborne=Animations?.Airborne;
        Play(enteredWithMoveIntent
            ?airborne?.MovingJumpLoop??airborne?.StandingJumpLoop
            :airborne?.StandingJumpLoop);
    }

}
