using Unity.Netcode;

// 非泛型基类用于统一收集不同数据类型的 Channel。
// Replicator 只认识这个基类，因此能在一个循环里处理 Input、State 等不同数据。
public abstract class ActorReplicationChannel
{
    public abstract ushort ChannelId { get; }
    public abstract ActorReplicationDirection Direction { get; }

    internal abstract bool Write(
        // in 表示只读引用：方法不能替换 context，同时避免复制整个值类型。
        in ActorReplicationContext context,
        // FastBufferWriter 只负责把数据写成字节，本身不会发送网络消息。
        // 它是一个指向 NGO 内部缓冲区的轻量包装，按值传递仍操作同一个内部 Handle。
        FastBufferWriter writer);

    internal abstract bool ReadAndApply(
        in ActorReplicationContext context,
        FastBufferReader reader,
        int payloadEndPosition);
}

public abstract class ActorReplicationChannel<TData> : ActorReplicationChannel
    // TData 必须是值类型，并实现 NGO 的网络序列化接口。
    where TData : struct, INetworkSerializable
{
    // sealed override 固定统一序列化流程，具体 Channel 只能决定“写什么”和“怎样应用”，
    // 不能绕过长度校验或直接操作 Replicator 的分发过程。
    internal sealed override bool Write(
        in ActorReplicationContext context,
        FastBufferWriter writer)
    {
        // out 同时承担两个返回值：bool 表示本 Tick 是否需要写，payload 返回实际数据。
        // out 要求 TryWrite 在所有返回路径上都给 payload 赋值。
        if(!TryWrite(in context,out TData payload))return false;

        // in payload 表示只读传入，序列化过程不应修改原始快照。
        writer.WriteNetworkSerializable(in payload);
        return true;
    }

    internal sealed override bool ReadAndApply(
        in ActorReplicationContext context,
        FastBufferReader reader,
        int payloadEndPosition)
    {
        // Reader 按 TData.NetworkSerialize 中完全相同的字段顺序还原数据。
        reader.ReadNetworkSerializable(out TData payload);
        // 必须刚好读完本 Channel 声明的长度，否则说明协议不匹配或数据包非法。
        if(reader.Position!=payloadEndPosition)return false;

        Apply(in context,in payload);
        return true;
    }

    protected abstract bool TryWrite(
        in ActorReplicationContext context,
        out TData payload);

    // in payload 防止 Apply 修改收到的快照；需要修正数据时应先复制到局部变量。
    protected abstract void Apply(
        in ActorReplicationContext context,
        in TData payload);
}
