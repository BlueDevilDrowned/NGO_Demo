using Unity.Netcode;

public struct LocomotionStateSnapshot : INetworkSerializable
{
    public LocomotionStateType StateType;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref StateType);
    }
}
