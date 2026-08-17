using Unity.Netcode;
using UnityEngine;

public struct ActorCameraSpanshot:INetworkSerializable
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref data.ViewYaw);
        serializer.SerializeValue(ref data.ViewPitch);
        serializer.SerializeValue(ref data.ViewOrigin);
        serializer.SerializeValue(ref data.ViewDirection);
    }

    public ActorCameraData data;
}
