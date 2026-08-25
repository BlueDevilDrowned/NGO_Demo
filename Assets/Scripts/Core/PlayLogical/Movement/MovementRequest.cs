using UnityEngine;

public struct MovementRequest
{
    public string Source;

    public Vector3 WorldPositionDelta;
    public float ForwardPositionDelta;
    public float YawDelta;
    public VerticalVelocityRequest verticalVelocity;
    public static MovementRequest Default=>new MovementRequest
    {
        Source="none",
        WorldPositionDelta=Vector3.zero,
        ForwardPositionDelta=0,
        YawDelta=0,
        verticalVelocity=new VerticalVelocityRequest
        {
            Mode=VerticalVelocityMode.None,
            Priority=0,
        }
    };
}
public enum VerticalVelocityMode
{
    None,
    Set,
    AddImpulse,
    Clear
}

public struct VerticalVelocityRequest
{
    public VerticalVelocityMode Mode;
    public float Value;
    public int Priority;
}
