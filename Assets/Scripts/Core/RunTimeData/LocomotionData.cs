using Unity.Netcode;
using UnityEngine;

public struct LocomotionData : INetworkSerializable
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        
    }
    
}
