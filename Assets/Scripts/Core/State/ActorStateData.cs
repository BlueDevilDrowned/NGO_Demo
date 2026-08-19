using System;
using UnityEngine;

[Serializable]
public struct ActorStateData
{
    public bool StartFootIsL;
    public LocomotionStateType LastMoveState;
    public Vector2 Parameter;
    public float ImpactSpeed;
}
