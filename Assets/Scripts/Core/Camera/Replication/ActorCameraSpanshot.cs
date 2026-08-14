using Unity.Netcode;
using UnityEngine;

public struct ActorCameraSpanshot:INetworkSerializable
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref data.ViewYaw);
        serializer.SerializeValue(ref data.ViewPitch);
    }

    public ActorCameraData data;
}
