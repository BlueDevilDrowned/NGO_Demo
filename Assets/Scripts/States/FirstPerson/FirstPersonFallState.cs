using UnityEngine;

public sealed class FirstPersonFallState : FirstPersonActorState
{
    public FirstPersonFallState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return !actor.movement.gravite.IsGrounded;
    }

    public override void Enter()
    {
        animation.PlayTransition(Animations.JumpLoop,AnimPlayOptions.Default);
    }

    public override void ServerTick()
    {
        GraviteModule gravity=actor.movement.gravite;
        if(!gravity.JustLanded)return;

        actor.simulation.stateData.ImpactSpeed=gravity.LastImpactSpeed;
        stateMachine.ChangeState(
            stateRegistry.GetState<FirstPersonLandState>());
    }

    public override void EvaluateMotion()
    {
        SubmitPlanarMovement(
            "FirstPersonFall",
            actor.actorSO.controllerSO.JumpSpeed);
    }
}
