using Unity.Netcode;

public struct ActorStateSnapshot : INetworkSerializable
{
    public ActorStateType StateType;
    public uint StateEnterTick;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref StateType);
        serializer.SerializeValue(ref StateEnterTick);
    }
}
