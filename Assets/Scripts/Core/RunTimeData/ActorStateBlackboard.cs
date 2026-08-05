using System;
using Unity.Netcode;
using UnityEngine;
[Serializable]
public struct ActorStateBlackboard : INetworkSerializable
{
    public bool StartFootIsL;
    public Vector2 Parameter;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref StartFootIsL);
        serializer.SerializeValue(ref Parameter);
    }
}
