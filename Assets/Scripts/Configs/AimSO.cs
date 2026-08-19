using System;
using UnityEngine;

[CreateAssetMenu(fileName="AimSO",menuName="Scriptable Objects/AimSO")]
public sealed class AimSO : ScriptableObject
{
    [Header("Body Turn")]
    public float AimIdleYawIgrone=45f;
    public float AimIdleYawMax=720f;
    public float AimMoveYawIgrone=45f;
    public float AimMoveYawMax=720f;

    [Header("Target")]
    [Min(1f)]public float TargetDistance=200f;
    public LayerMask TargetCollisionMask=~0;

}
