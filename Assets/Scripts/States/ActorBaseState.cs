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

        return HasAnimation(standing.Run)
            ?standing.Run
            :standing.Jog;
    }

    protected void Play(TransitionAsset transition)
    {
        if(transition!=null)
            animation.PlayTransition(transition,AnimPlayOptions.Default);
    }

    private static bool HasAnimation(DirectionalLocomotionAnimations group)
    {
        return group!=null&&group.HasAnyAnimation;
    }

    protected Vector2 GetLocalMoveDirection()
    {
        Vector3 localDirection=actor.player.InverseTransformDirection(
            actor.simulation.locomotionData.DesiredWorldMoveDirection);
        return new Vector2(localDirection.x,localDirection.z);
    }

    protected bool TrySubmitRootMotion(RootMotionAnimation animationData)
    {
        if(animationData.RootData==null||!animationData.RootData.IsBaked||
           actor.motionDriver==null)
            return false;

        actor.motionDriver.SubmitClipMotion(animationData.RootData,animation);
        return true;
    }

    protected static void ApplyEndFootPhase(
        RootMotionData data,
        ref ActorStateData stateData)
    {
        if(data==null||!data.IsBaked)return;

        if(data.EndFootPhase==BakedFootPhase.LeftFootDown)
            stateData.StartFootIsL=true;
        else if(data.EndFootPhase==BakedFootPhase.RightFootDown)
            stateData.StartFootIsL=false;
    }

}
