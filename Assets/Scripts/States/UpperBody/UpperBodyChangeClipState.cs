public sealed class UpperBodyChangeClipState : WeaponUpperBodyState
{
    public UpperBodyChangeClipState(Actor actor) :
        base(actor,UpperBodyStateType.ChangeClip)
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

        if(ConsumeAction(UpperBodyActionRequest.ChangeClip))
        {
            TransitionTo(ResolveChangeClipState());
            return;
        }

        if(IsProne)
        {
            TransitionTo(UpperBodyStateType.ProneChangeClip);
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
