using Animancer;
using UnityEngine;

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
    protected const int Layer=1;
    protected readonly Actor actor;
    protected IAnimationFacade animation=>actor.animationFacade;
    protected UpperBodyStateMachine stateMachine=>actor.upperBodyStateSystem.Machine;
    protected UpperBodyStateRegistry stateRegistry=>actor.upperBodyStateSystem.Registry;

    public UpperBodyStateType StateType{get;}

    protected WeaponUpperBodyStateAnimation Configuration=>
        actor.weaponEquipment?.CurrentDefinition?.animationConfig?.
            ThirdPersonUpperBody?.GetState(StateType);

    public override float NormalizedTime=>
        animation?.GetLayerNormalizedTime(Layer)??0f;

    protected UpperBodyState(Actor actor,UpperBodyStateType stateType)
    {
        this.actor=actor??throw new System.ArgumentNullException(nameof(actor));
        StateType=stateType;
    }

    protected void PlayConfiguredAnimation()
    {
        TransitionAsset clip=Configuration?.Clip;
        if(clip==null)
        {
            animation?.StopLayer(Layer);
            animation?.SetLayerWeight(Layer,0f,0.1f);
            return;
        }

        AnimPlayOptions options=AnimPlayOptions.Default;
        options.Layer=Layer;
        animation?.PlayTransition(clip,options);
        ApplyConfiguredWeight();
    }

    protected void ApplyConfiguredWeight()
    {
        animation?.SetLayerWeight(
            Layer,
            Mathf.Clamp01(Configuration?.GlobalWeight??0f),
            0.1f);
    }
}
