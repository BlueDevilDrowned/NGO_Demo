using System;
using UnityEngine;

[CreateAssetMenu(fileName="AimSO",menuName="Scriptable Objects/AimSO")]
public sealed class AimSO : ScriptableObject
{
    [Header("Vertical Angle")]
    [Range(-89f,0f)]public float MinPitch=-50f;
    [Range(0f,89f)]public float MaxPitch=70f;
    [Header("State")]
    public float AimIdleYawIgrone=45;
    public float AimIdleYawMax=720;
    public float AimMoveYawIgrone=45;
    public float AimMoveYawMax=720;

    [Header("Look Speed")]
    public AimRotationSpeed PointerSensitivity=new(0.12f,0.12f);
    public AimRotationSpeed StickDegreesPerSecond=new(180f,120f);

    [Header("Target")]
    [Min(1f)]public float TargetDistance=200f;
    public LayerMask TargetCollisionMask=~0;

    [Header("Presentation")]
    [Min(0f)]public float RemoteRotationSharpness=20f;
    [Min(0f)]public float RemoteTargetSharpness=20f;
    [Min(0f)]public float RigBlendSpeed=8f;

    public float ClampPitch(float pitch)
    {
        float min=Mathf.Min(MinPitch,MaxPitch);
        float max=Mathf.Max(MinPitch,MaxPitch);
        return Mathf.Clamp(pitch,min,max);
    }
}

[Serializable]
public struct AimRotationSpeed
{
    [Min(0f)]public float Horizontal;
    [Min(0f)]public float Vertical;

    public AimRotationSpeed(float horizontal,float vertical)
    {
        Horizontal=horizontal;
        Vertical=vertical;
    }
}
