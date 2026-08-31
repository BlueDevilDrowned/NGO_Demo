using System;
using UnityEngine;
[Serializable]
public struct LocomotionData
{
    public Vector3 DesiredWorldMoveDirection;
    public float DesiredLocalMoveAngle;

    public LocomotionStateType stateType;
}
public enum LocomotionStateType
{
    Idle=0,
    Walk=1,
    Run=2,
    Sprint=3,
}
