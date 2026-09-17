using UnityEngine;

public class MovementMotor
{
    private readonly Actor actor;

    public MovementMotor(Actor actor)
    {
        this.actor=actor;
    }

    public Vector3 LastVelocity{get;private set;}

    public void Execute(MovementResult result)
    {
        actor.rootPoseSystem.RotateAuthoritative(result.YawDelta);
        ActorRootPose movementFrame=actor.rootPoseSystem.AuthoritativePose;

        Vector3 finalPositionDelta=
            result.WorldPositionDelta+
            movementFrame.Forward*result.ForwardPositionDelta;

        actor.characterController.Move(finalPositionDelta);
        LastVelocity=finalPositionDelta/Mathf.Max(TickTime.deltaTime,Mathf.Epsilon);
    }
}
