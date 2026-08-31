public abstract class WeaponUpperBodyState : UpperBodyState
{
    protected UpperBodyStateSystem System=>actor.upperBodyStateSystem;
    protected bool IsProne=>System.IsProne;
    protected bool HasConfiguredClip=>Configuration?.Clip!=null;

    protected WeaponUpperBodyState(
        Actor actor,
        UpperBodyStateType stateType) : base(actor,stateType)
    {
    }

    public override void Enter()
    {
        PlayState();
    }

    public override void ApplyParameter()
    {
        ApplyWeight();
    }

    protected void PlayState()
    {
        PlayConfiguredAnimation();
    }

    protected void ApplyWeight()
    {
        ApplyConfiguredWeight();
    }

    protected bool ConsumeAction(UpperBodyActionRequest action)
    {
        return System.ConsumeAction(action);
    }

    protected bool TransitionTo(UpperBodyStateType target)
    {
        return System.ChangeStateFromStateLogic(target);
    }

    protected UpperBodyStateType ResolveIdleState()
    {
        return IsProne
            ?UpperBodyStateType.ProneIdle
            :UpperBodyStateType.Idle;
    }

    protected UpperBodyStateType ResolveGetWeaponState()
    {
        return IsProne
            ?UpperBodyStateType.ProneGetWeapon
            :UpperBodyStateType.GetWeapon;
    }

    protected UpperBodyStateType ResolveChangeClipState()
    {
        return IsProne
            ?UpperBodyStateType.ProneChangeClip
            :UpperBodyStateType.ChangeClip;
    }
}
