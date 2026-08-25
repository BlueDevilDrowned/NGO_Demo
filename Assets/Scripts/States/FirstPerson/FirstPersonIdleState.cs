using UnityEngine;

public sealed class FirstPersonIdleState : FirstPersonActorState
{
    public FirstPersonIdleState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.PlayTransition(Animations.Idle,AnimPlayOptions.Default);
    }

    public override void ServerTick()
    {
        ActorBaseState target=ResolveGroundedState();
        if(!ReferenceEquals(target,this))
        {
            stateMachine.ChangeState(target);
            return;
        }
        //跟随
    }
    public override void EvaluateMotion()
    {
        float maxYawDelta=
            Mathf.Max(0f,actor.actorSO.animationSO.firstPersonIdleTurnAngle)*
            TickTime.deltaTime;

        MovementRequest request=MovementRequest.Default;
        request.Source="FirstPersonIdle";
        request.YawDelta=Mathf.Clamp(
            actor.simulation.CameraBodyYawDelta,
            -maxYawDelta,
            maxYawDelta);

        actor.movement.Submit(in request);
    }
}
