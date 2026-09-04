public sealed class UpperBodyProneIdleState : WeaponUpperBodyState
{
    public UpperBodyProneIdleState(Actor actor) :
        base(actor,UpperBodyStateType.ProneIdle)
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

        if(!IsProne)
        {
            TransitionTo(UpperBodyStateType.Idle);
            return;
        }

        if(actor.simulation.inputData.IsHeld(InputButtons.InputAttack))
            actor.weapon.TryFire();
    }
}
