using System;
using UnityEngine;

[Serializable]
public struct ActorRootPose
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;

    public ActorRootPose(
        Vector3 position,
        Quaternion rotation,
        Vector3 scale)
    {
        Position=position;
        Rotation=rotation;
        Scale=scale;
    }

    public Vector3 Forward=>Rotation*Vector3.forward;
    public Vector3 Right=>Rotation*Vector3.right;
    public Vector3 Up=>Rotation*Vector3.up;
}
