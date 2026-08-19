using Unity.Netcode;

public struct UpperBodyStateSnapshot : INetworkSerializable
{
    public UpperBodyStateType StateType;
    public uint StateEnterTick;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref StateType);
        serializer.SerializeValue(ref StateEnterTick);
    }
}
