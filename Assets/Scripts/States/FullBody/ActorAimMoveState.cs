using UnityEngine;

public class ActorAimMoveState : ActorBaseState
{
    public ActorAimMoveState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        
        animation.PlayTransition(actor.animancerData.Aiming.Walk,AnimPlayOptions.Default);
        Vector3 localDir = actor.player.InverseTransformDirection(actor.runTimeData.locomotion.DesiredWorldMoveDirection);
        Vector2 parameter=new(localDir.x,localDir.z);
        actor.runTimeData.blackboard.Parameter=parameter;
        animation.SetMixerParameter(actor.runTimeData.blackboard.Parameter);

        //
        actor.actorAudio.PlayLoop("Walk");
    }
    public override void Exit()
    {
        actor.actorAudio.StopLoop();
    }

    public override void ServerTick()
    {
        if(!actor.runTimeData.WantAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<ActorMoveLoopState>());
            return;
        }
        if(!actor.runTimeData.WantMove)
        {
            //前往start
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimIdleState>());
            return;
        }

        
    }

    public override void ApplyParameter()
    {
        //根据输入和朝向
        Vector3 localDirection=actor.player.InverseTransformDirection(
            actor.runTimeData.locomotion.DesiredWorldMoveDirection);
        Vector2 targetParameter=new(localDirection.x,localDirection.z);
        actor.runTimeData.blackboard.Parameter=Vector2.MoveTowards(
            actor.runTimeData.blackboard.Parameter,
            targetParameter,
            actor.animationSO.Walk_Loop_SmoothFactor*TickTime.deltaTime);
        animation.SetMixerParameter(actor.runTimeData.blackboard.Parameter);
    }
    public override void EvaluateMotion()
    {
        
        actor.aim.TrySubmitBodyTurn(actor.aimSO.AimMoveYawIgrone,actor.aimSO.AimMoveYawMax);
        //移动
        MovementRequest request=new()
        {
            Source="AimMove",
            WorldPositionDelta=
                actor.runTimeData.locomotion.DesiredWorldMoveDirection*
                actor.controllerSO.AimWalkSpeed*
                Mathf.Clamp01(actor.runTimeData.Input.InputMove.magnitude)*
                TickTime.deltaTime,

            ForwardPositionDelta=0f,
            YawDelta=0f,
        };

        actor.movement.Submit(request);


    }
}
