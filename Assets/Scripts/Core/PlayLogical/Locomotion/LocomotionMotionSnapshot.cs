using Unity.Netcode;
using UnityEngine;

public struct LocomotionMotionSnapshot : INetworkSerializable
{
    public Vector3 DesiredWorldMoveDirection;
    public float DesiredLocalMoveAngle;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref DesiredWorldMoveDirection);
        serializer.SerializeValue(ref DesiredLocalMoveAngle);
    }
}
