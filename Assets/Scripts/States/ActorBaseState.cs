public abstract class ActorBaseState:BaseState
{

    protected Actor actor;
    protected IAnimationFacade animation;
    protected StateMachine stateMachine=>actor.actorStateSystem.Machine;
    protected ActorStateRegistry stateRegistry=>actor.actorStateSystem.Registry;
    public override float NormalizedTime=>animation.CurrentNormalizedTime;

    public ActorBaseState(Actor actor)
    {
        this.actor=actor;
        animation=actor.animationFacade;
    }

}
