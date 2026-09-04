public sealed class FirstPersonLandState : FirstPersonActorState
{
    public FirstPersonLandState(Actor actor) : base(actor)
    {
    }

    public override bool CanEnterFrom(BaseState currentState)
    {
        return IsFullBodyState(ActorStateType.Land);
    }

    public override void Enter()
    {
        FirstPersonWeaponAirborneAnimations airborne=Animations?.Airborne;
        bool aiming=actor.aimSystem?.IsAiming==true;
        Play(aiming
            ?airborne?.AimJumpLand??airborne?.JumpLand
            :airborne?.JumpLand);
    }
    public override void PresentationUpdate(float deltaTime)
    {
        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
        {
            Play(Animations.Combat.Attack);
        }
    }
}
