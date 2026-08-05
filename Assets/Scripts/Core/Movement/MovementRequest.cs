using UnityEngine;

public struct MovementRequest
{
    public string Source;

    public Vector3 WorldPositionDelta;
    public float ForwardPositionDelta;
    public float YawDelta;
}
