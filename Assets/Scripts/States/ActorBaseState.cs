using UnityEngine;

public class ActorBaseState:BaseState
{
    protected Actor actor;
    protected IAnimationFacade animation;
    protected ActorBaseState(Actor actor)
    {
        this.actor=actor;
        animation=actor.animationFacade;
    }
}
