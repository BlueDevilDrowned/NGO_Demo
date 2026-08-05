using System;
using System.Collections.Generic;
using Unity.Netcode;

public sealed class ActorSnapshotReplicator
{
    // List 保留注册顺序，Dictionary 用 ChannelId 在接收时快速找到处理者。
    // readonly 只禁止字段改指向另一个集合，不代表集合内容不可增删。
    private readonly List<ActorReplicationChannel> channels=new();
    private readonly Dictionary<ushort,ActorReplicationChannel> channelsById=new();

    // 对外只暴露只读视图，注册和注销仍必须经过本类的方法维护两份集合的一致性。
    public IReadOnlyList<ActorReplicationChannel> Channels=>channels;

    public bool Register(ActorReplicationChannel channel)
    {
        if(channel==null)return false;
        if(channelsById.ContainsKey(channel.ChannelId))return false;

        channels.Add(channel);
        channelsById.Add(channel.ChannelId,channel);
        return true;
    }

    public bool Unregister(ushort channelId)
    {
        if(!channelsById.TryGetValue(channelId,out ActorReplicationChannel channel))
            return false;

        channelsById.Remove(channelId);
        channels.Remove(channel);
        return true;
    }

    public void Clear()
    {
        channels.Clear();
        channelsById.Clear();
    }

    public ushort WriteAll(
        ActorReplicationDirection direction,
        in ActorReplicationContext context,
        FastBufferWriter writer)
    {
        // 数据包格式：
        // [Channel数量][ChannelId][Payload长度][Payload]...
        // 先写 0 占位，所有 Channel 写完后再回填真实数量。
        int countPosition=writer.Position;
        writer.WriteValueSafe((ushort)0);

        ushort writtenCount=0;
        for(int i=0;i<channels.Count;i++)
        {
            ActorReplicationChannel channel=channels[i];
            if(channel.Direction!=direction)continue;

            int recordPosition=writer.Position;

            writer.WriteValueSafe(channel.ChannelId);
            // Payload 写入前还不知道最终字节数，因此先占一个 int，写完再回填。
            int lengthPosition=writer.Position;
            writer.WriteValueSafe(0);
            int payloadPosition=writer.Position;

            if(!channel.Write(in context,writer))
            {
                // 当前 Channel 本 Tick 不提交，撤销已经写入的 Id 和长度占位。
                writer.Truncate(recordPosition);
                continue;
            }

            int endPosition=writer.Position;
            int payloadLength=endPosition-payloadPosition;
            // Seek 只移动写入位置；回填完成后必须回到包尾继续写下一个 Channel。
            writer.Seek(lengthPosition);
            writer.WriteValueSafe(payloadLength);
            writer.Seek(endPosition);
            writtenCount++;
        }

        int packetEndPosition=writer.Position;
        writer.Seek(countPosition);
        writer.WriteValueSafe(writtenCount);
        writer.Seek(packetEndPosition);
        return writtenCount;
    }

    public bool ReadAllAndApply(
        ActorReplicationDirection direction,
        in ActorReplicationContext context,
        FastBufferReader reader)
    {
        // TryBeginRead 在读取前检查剩余字节，防止损坏的数据包越界。
        if(!reader.TryBeginRead(sizeof(ushort)))return false;
        reader.ReadValueSafe(out ushort channelCount);

        for(int i=0;i<channelCount;i++)
        {
            if(!reader.TryBeginRead(sizeof(ushort)+sizeof(int)))return false;

            reader.ReadValueSafe(out ushort channelId);
            reader.ReadValueSafe(out int payloadLength);
            if(payloadLength<0||payloadLength>reader.Length-reader.Position)
                return false;

            int payloadEndPosition=reader.Position+payloadLength;
            if(channelsById.TryGetValue(channelId,out ActorReplicationChannel channel))
            {
                // 即使包里伪造了另一个方向的 ChannelId，也不能跨方向应用。
                if(channel.Direction!=direction)
                {
                    reader.Seek(payloadEndPosition);
                    continue;
                }

                try
                {
                    if(!channel.ReadAndApply(
                        in context,
                        reader,
                        payloadEndPosition))return false;
                }
                catch(OverflowException)
                {
                    // NGO Reader 在字段不足时抛出 OverflowException，将整个包判定为无效。
                    return false;
                }
            }

            // 未注册的 Channel 可以依靠 PayloadLength 跳过，便于两端逐步增加新 Channel。
            reader.Seek(payloadEndPosition);
        }

        return reader.Position==reader.Length;
    }
}
