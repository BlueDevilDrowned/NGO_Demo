using UnityEngine;

public sealed class FirstPersonFallState : FirstPersonActorState
{
    public FirstPersonFallState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return IsFullBodyState(ActorStateType.Fall);
    }

    public override void Enter()
    {
        FirstPersonWeaponAirborneAnimations airborne=Animations?.Airborne;
        bool aiming=actor.aimSystem?.IsAiming==true;
        Play(aiming
            ?airborne?.AimJumpLoop??airborne?.JumpLoop
            :airborne?.JumpLoop);
    }
    public override void PresentationUpdate(float deltaTime)
    {
        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
        {
            Play(Animations.Combat.Attack);
        }
    }
}
