using UnityEngine;

public sealed class FirstPersonJumpState : FirstPersonActorState
{
    public FirstPersonJumpState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return IsFullBodyState(ActorStateType.Jump);
    }

    public override void Enter()
    {
        FirstPersonWeaponAirborneAnimations airborne=Animations?.Airborne;
        bool aiming=actor.aimSystem?.IsAiming==true;
        Play(aiming
            ?airborne?.AimJumpStart??airborne?.JumpStart
            :airborne?.JumpStart);
    }
    public override void PresentationUpdate(float deltaTime)
    {
        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
        {
            Play(Animations.Combat.Attack);
        }
    }
}
