using Unity.Netcode;

public struct ActorInputCommand : INetworkSerializable
{
    // BufferSerializer 会根据 T 是 Reader 还是 Writer，决定从字段读取还是向字段写入。
    // ref 很关键：写入网络时序列化器读取字段；读取网络时序列化器要能修改字段。
    // 两端必须按完全相同的顺序调用 SerializeValue，这个顺序就是二进制协议的一部分。
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Data.InputMove);
        serializer.SerializeValue(ref Data.InputLook);
        serializer.SerializeValue(ref Data.Held);
        serializer.SerializeValue(ref Data.Pressed);
        serializer.SerializeValue(ref Data.ViewYaw);
    }
    public uint Tick;
    public ActorInputData Data;
}
