public sealed class UpperBodyProneGetWeaponState : WeaponUpperBodyState
{
    public UpperBodyProneGetWeaponState(Actor actor) :
        base(actor,UpperBodyStateType.ProneGetWeapon)
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

        if(!IsProne)
        {
            TransitionTo(UpperBodyStateType.GetWeapon);
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
