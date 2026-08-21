using UnityEngine;

public class ActorAimMoveState : ActorBaseState
{
    public ActorAimMoveState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        if(actor.IsOwner)
            actor.aimSystem.SetPresentationAim(true);

        if(actor.IsServer)
            actor.simulation.aimData.IsAiming=true;
        animation.PlayTransition(actor.actorSO.animancerData.ThirdPerson.Aiming.Walk,AnimPlayOptions.Default);
        Vector3 localDir = actor.player.InverseTransformDirection(actor.simulation.locomotionData.DesiredWorldMoveDirection);
        Vector2 parameter=new(localDir.x,localDir.z);
        actor.simulation.stateData.Parameter=parameter;
        animation.SetMixerParameter(actor.simulation.stateData.Parameter);

        //
        actor.audioSystem.PlayLoop("Walk");
    }
    public override void Exit()
    {
        if(actor.IsOwner)
        actor.aimSystem.SetPresentationAim(false);

        if(actor.IsServer)
        actor.simulation.aimData.IsAiming=false;

        actor.audioSystem.StopLoop();
    }

    public override void ServerTick()
    {
        if(!actor.simulation.CanAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<ActorMoveLoopState>());
            return;
        }
        if(!actor.simulation.WantMove)
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
            actor.simulation.locomotionData.DesiredWorldMoveDirection);
        Vector2 targetParameter=new(localDirection.x,localDirection.z);
        actor.simulation.stateData.Parameter=Vector2.MoveTowards(
            actor.simulation.stateData.Parameter,
            targetParameter,
            actor.actorSO.animationSO.Walk_Loop_SmoothFactor*TickTime.deltaTime);
        animation.SetMixerParameter(actor.simulation.stateData.Parameter);
    }
    public override void EvaluateMotion()
    {
        
        AimSO config=actor.actorSO.aimSO;
        if(config!=null)
            actor.aimSystem.TrySubmitBodyTurn(
                config.AimMoveYawIgrone,
                config.AimMoveYawMax);
        //移动
        MovementRequest request=new()
        {
            Source="AimMove",
            WorldPositionDelta=
                actor.simulation.locomotionData.DesiredWorldMoveDirection*
                actor.actorSO.controllerSO.AimWalkSpeed*
                Mathf.Clamp01(actor.simulation.inputData.InputMove.magnitude)*
                TickTime.deltaTime,

            ForwardPositionDelta=0f,
            YawDelta=0f,
        };

        actor.movement.Submit(request);


    }
}
