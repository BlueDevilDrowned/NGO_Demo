using UnityEngine;

public struct LocomotionData
{
    public Vector3 DesiredWorldMoveDirection;
    public float DesiredLocalMoveAngle;

    public LocomotionStateType stateType;
}
public enum LocomotionStateType
{
    Idle,
    Walk,
    Jog,
}