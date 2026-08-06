using UnityEngine;

public struct MovementRequest
{
    public string Source;

    public Vector3 WorldPositionDelta;
    public float ForwardPositionDelta;
    public float YawDelta;
    public static MovementRequest Default=>new MovementRequest
    {
        Source="none",
        WorldPositionDelta=Vector3.zero,
        ForwardPositionDelta=0,
        YawDelta=0,
    };
}
