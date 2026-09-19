using Unity.Netcode;
using UnityEngine;

public struct AimTargetStateSnapshot : INetworkSerializable
{
    public Vector3 TargetPosition;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref TargetPosition);
    }
}
