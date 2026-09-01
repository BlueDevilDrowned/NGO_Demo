using System;
using UnityEngine;
[Serializable]
public struct   ActorCameraData
{
    public float ViewYaw;
    public float ViewPitch;
    public Vector3 ViewOrigin;
    public Vector3 ViewDirection;
}

public static class ActorCameraDataUtility
{
    public static Vector3 CalculateViewDirection(float yaw,float pitch)
    {
        return Quaternion.Euler(pitch,yaw,0f)*Vector3.forward;
    }

    public static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}

public enum CameraViewMode
{
    FreeLook,
    Aim,
}
public enum CameraPerspectiveMode
{
    ThirdPerson,
    FirstPerson,
}
