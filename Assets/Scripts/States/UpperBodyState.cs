public enum UpperBodyStateType
{
    Idle,
    GetWeapon,
    ChangeClip,
    ProneIdle,
    ProneGetWeapon,
    ProneChangeClip,
}

[System.Flags]
public enum UpperBodyActionRequest
{
    None=0,
    GetWeapon=1<<0,
    ChangeClip=1<<1,
}

public abstract class UpperBodyState : BaseState
{
    protected readonly Actor actor;
    protected UpperBodyStateMachine stateMachine=>actor.upperBodyStateSystem.Machine;
    protected UpperBodyStateRegistry stateRegistry=>actor.upperBodyStateSystem.Registry;

    public UpperBodyStateType StateType{get;}

    protected WeaponUpperBodyStateAnimation Configuration=>
        actor.weaponEquipment?.CurrentDefinition?.animationConfig?.
            ThirdPersonUpperBody?.GetState(StateType);

    public override float NormalizedTime=>
        stateMachine.AnimationNormalizedTime;

    protected UpperBodyState(Actor actor,UpperBodyStateType stateType)
    {
        this.actor=actor??throw new System.ArgumentNullException(nameof(actor));
        StateType=stateType;
    }

    protected void PlayConfiguredAnimation()
    {
        stateMachine.PlayAnimation(Configuration);
    }

    protected void ApplyConfiguredWeight()
    {
        stateMachine.ApplyAnimationWeight(Configuration);
    }
}
