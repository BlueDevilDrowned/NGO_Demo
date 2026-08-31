using Animancer;
using UnityEngine;

public abstract class ActorBaseState:BaseState
{

    protected Actor actor;
    protected IAnimationFacade animation;
    protected StateMachine stateMachine=>actor.actorStateSystem.Machine;
    protected ActorStateRegistry stateRegistry=>actor.actorStateSystem.Registry;
    protected FullBodyAnimationSO Animations=>actor.actorSO.fullBodyAnimation;
    public override float NormalizedTime=>animation.CurrentNormalizedTime;

    public ActorBaseState(Actor actor)
    {
        this.actor=actor;
        animation=actor.animationFacade;
    }

    protected DirectionalLocomotionAnimations GetLocomotionAnimations(
        LocomotionStateType state)
    {
        StandingFullBodyAnimations standing=Animations?.Standing;
        if(standing==null)return null;

        if(state==LocomotionStateType.Walk)
            return standing.Walk;

        return standing.RunSprint;
    }

    protected void Play(TransitionAsset transition)
    {
        if(transition!=null)
            animation.PlayTransition(transition,AnimPlayOptions.Default);
    }

}
