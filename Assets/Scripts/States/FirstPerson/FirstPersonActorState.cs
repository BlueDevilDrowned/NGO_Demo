using UnityEngine;

public abstract class FirstPersonActorState : ActorBaseState
{
    protected FirstPersonAnimationTransitions Animations=>
        actor.actorSO.animancerData.FirstPerson;

    protected FirstPersonActorState(Actor actor) : base(actor)
    {
    }

    protected ActorBaseState ResolveGroundedState()
    {
        if(actor.simulation.CanAim)
        {
            return actor.simulation.WantMove
                ?stateRegistry.GetState<FirstPersonAimMoveState>()
                :stateRegistry.GetState<FirstPersonAimIdleState>();
        }

        if(!actor.simulation.WantMove)
            return stateRegistry.GetState<FirstPersonIdleState>();

        return actor.simulation.locomotionData.stateType==LocomotionStateType.Jog
            ?stateRegistry.GetState<FirstPersonSprintState>()
            :stateRegistry.GetState<FirstPersonMoveState>();
    }

    protected void SetAiming(bool isAiming)
    {
        if(actor.IsOwner)
            actor.aimSystem.SetPresentationAim(isAiming);

        if(actor.IsServer)
            actor.simulation.aimData.IsAiming=isAiming;
    }

    protected void InitializeMixerParameter()
    {
        actor.simulation.stateData.Parameter=GetLocalMoveParameter();
        animation.SetMixerParameter(actor.simulation.stateData.Parameter);
    }

    protected void UpdateMixerParameter()
    {
        float smoothFactor=actor.actorSO.animationSO.Walk_Loop_SmoothFactor;
        actor.simulation.stateData.Parameter=Vector2.MoveTowards(
            actor.simulation.stateData.Parameter,
            GetLocalMoveParameter(),
            smoothFactor*TickTime.deltaTime);
        animation.SetMixerParameter(actor.simulation.stateData.Parameter);
    }

    protected void SubmitPlanarMovement(string source,float speed)
    {
        MovementRequest request=MovementRequest.Default;
        request.Source=source;
        request.WorldPositionDelta=
            actor.simulation.locomotionData.DesiredWorldMoveDirection*
            speed*
            Mathf.Clamp01(actor.simulation.inputData.InputMove.magnitude)*
            TickTime.deltaTime;
        actor.movement.Submit(in request);
    }

    private Vector2 GetLocalMoveParameter()
    {
        Vector3 localDirection=actor.player.InverseTransformDirection(
            actor.simulation.locomotionData.DesiredWorldMoveDirection);
        return new Vector2(localDirection.x,localDirection.z);
    }
}
