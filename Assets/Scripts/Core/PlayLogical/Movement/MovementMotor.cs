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
        Transform movementFrame=actor.transform;
        Vector3 up=movementFrame.up;

        Quaternion yawRotation=Quaternion.AngleAxis(result.YawDelta,up);
        actor.transform.rotation=yawRotation*actor.transform.rotation;

        Vector3 finalPositionDelta=
            result.WorldPositionDelta+
            movementFrame.forward*result.ForwardPositionDelta;

        actor.characterController.Move(finalPositionDelta);
        LastVelocity=finalPositionDelta/Mathf.Max(TickTime.deltaTime,Mathf.Epsilon);
    }
}
