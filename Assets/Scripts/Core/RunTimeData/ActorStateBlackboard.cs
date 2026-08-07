using System;
using UnityEngine;

[Serializable]
public struct ActorStateBlackboard
{
    public bool StartFootIsL;
    public LocomotionStateType LastMoveState;
    public Vector2 Parameter;
    public float ImpactSpeed;
}
