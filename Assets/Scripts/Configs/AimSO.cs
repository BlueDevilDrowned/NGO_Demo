using System;
using UnityEngine;

[CreateAssetMenu(fileName="AimSO",menuName="Scriptable Objects/AimSO")]
public sealed class AimSO : ScriptableObject
{
    [Header("Target")]
    [Min(1f)]public float TargetDistance=200f;
    public LayerMask TargetCollisionMask=~0;

}
