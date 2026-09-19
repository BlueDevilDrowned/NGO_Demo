using Unity.Netcode;

public struct ActorInputSnapshot : INetworkSerializable
{
    // BufferSerializer 会根据 T 是 Reader 还是 Writer，决定从字段读取还是向字段写入。
    // ref 很关键：写入网络时序列化器读取字段；读取网络时序列化器要能修改字段。
    // 两端必须按完全相同的顺序调用 SerializeValue，这个顺序就是二进制协议的一部分。
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref EstimatedServerTick);
        serializer.SerializeValue(ref PresentedServerTick);
        serializer.SerializeValue(ref Data.InputMove);
        serializer.SerializeValue(ref Data.InputLook);
        serializer.SerializeValue(ref Data.InputScroll);
        serializer.SerializeValue(ref Data.InteractionTarget);
        serializer.SerializeValue(ref Data.InteractionOption);
        serializer.SerializeValue(ref Data.ClientShotId);
        serializer.SerializeValue(ref Data.Held);
        serializer.SerializeValue(ref Data.Pressed);
    }
    /// <summary>发送该输入快照时的本地输入 Tick。</summary>
    public uint Tick;
    /// <summary>客户端发送时对服务器当前时间的估算，用于没有表现 Tick 时回退。</summary>
    public uint EstimatedServerTick;
    /// <summary>玩家实际看到并据此产生输入的延迟表现 Tick，射击回溯优先使用它。</summary>
    public uint PresentedServerTick;
    public ActorInputData Data;
}
