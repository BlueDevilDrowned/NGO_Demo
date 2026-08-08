public abstract class UpperBodyState : BaseState
{
    protected const int Layer=1;
    protected readonly Actor actor;
    protected IAnimationFacade animation=>actor.animationFacade;
    protected UpperBodyStateMachine stateMachine=>actor.upperBodyStateMachine;
    protected UpperBodyStateRegistry stateRegistry=>actor.UpperBodyStateRegistry;

    public override float NormalizedTime=>
        animation.GetLayerNormalizedTime(Layer);

    protected UpperBodyState(Actor actor)
    {
        this.actor=actor??throw new System.ArgumentNullException(nameof(actor));
    }

}
