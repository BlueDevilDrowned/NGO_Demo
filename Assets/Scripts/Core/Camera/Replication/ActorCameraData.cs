using UnityEngine;

public struct   ActorCameraData
{
    public float ViewYaw;
    public float ViewPitch;
    public Vector3 ViewOrigin;
    public Vector3 ViewDirection;
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
