public sealed class UpperBodyProneChangeClipState : WeaponUpperBodyState
{
    public UpperBodyProneChangeClipState(Actor actor) :
        base(actor,UpperBodyStateType.ProneChangeClip)
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

        if(!IsProne)
        {
            TransitionTo(UpperBodyStateType.ChangeClip);
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
