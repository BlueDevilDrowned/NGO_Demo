public sealed class UpperBodyIdleState : WeaponUpperBodyState
{
    public UpperBodyIdleState(Actor actor) :
        base(actor,UpperBodyStateType.Idle)
    {
    }

    public override void ServerTick()
    {
        if(ConsumeAction(UpperBodyActionRequest.GetWeapon))
        {
            TransitionTo(ResolveGetWeaponState());
            return;
        }

        if(ConsumeAction(UpperBodyActionRequest.ChangeClip))
        {
            TransitionTo(ResolveChangeClipState());
            return;
        }

        if(IsProne)
        {
            TransitionTo(UpperBodyStateType.ProneIdle);
            return;
        }

        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
            actor.weapon.TryFire();
    }
}
