public abstract class UpperBodyState : BaseState
{
    protected const int Layer=1;
    protected readonly Actor actor;
    protected IAnimationFacade animation=>actor.animationFacade;
    protected ThirdPersonUpperBodyAnimations Animations=>
        actor.weaponEquipment?.CurrentDefinition?.animationConfig?.
            ThirdPersonUpperBody;
    protected UpperBodyStateMachine stateMachine=>actor.upperBodyStateSystem.Machine;
    protected UpperBodyStateRegistry stateRegistry=>actor.upperBodyStateSystem.Registry;

    public override float NormalizedTime=>
        animation.GetLayerNormalizedTime(Layer);

    protected UpperBodyState(Actor actor)
    {
        this.actor=actor??throw new System.ArgumentNullException(nameof(actor));
    }

    protected void PlayBasePose(Animancer.TransitionAsset transition)
    {
        if(transition==null)
        {
            animation.StopLayer(Layer);
            animation.SetLayerWeight(Layer,0f,0.1f);
            return;
        }

        AnimPlayOptions options=AnimPlayOptions.Default;
        options.Layer=Layer;
        animation.PlayTransition(transition,options);
        animation.SetLayerWeight(Layer,1f,0.1f);
    }

}
