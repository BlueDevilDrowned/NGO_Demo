public sealed class UpperBodyGetWeaponState : WeaponUpperBodyState
{
    public UpperBodyGetWeaponState(Actor actor) :
        base(actor,UpperBodyStateType.GetWeapon)
    {
    }

    public override void Enter()
    {
        base.Enter();
        stateMachine.SetOnEndCallback(Complete);
    }

    public override void ServerTick()
    {
        if(ConsumeAction(UpperBodyActionRequest.GetWeapon))
        {
            TransitionTo(ResolveGetWeaponState());
            return;
        }

        if(IsProne)
        {
            TransitionTo(UpperBodyStateType.ProneGetWeapon);
            return;
        }

        if(!HasConfiguredClip)
            Complete();
    }

    private void Complete()
    {
        TransitionTo(ResolveIdleState());
    }
}
