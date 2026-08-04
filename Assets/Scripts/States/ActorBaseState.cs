public abstract class ActorBaseState:BaseState
{

    public abstract ActorStateType StateType{get;}
    protected Actor actor;
    protected IAnimationFacade animation;
    protected StateMachine stateMachine;
    protected ActorStateRegistry stateRegistry;
    public override float NormalizedTime=>animation.CurrentNormalizedTime;

    public ActorBaseState(Actor actor)
    {
        this.actor=actor;
        animation=actor.animationFacade;
        stateMachine=actor.stateMachine;
        stateRegistry=actor.StateRegistry;
    }
}
