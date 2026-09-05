using System;

/// <summary>Derives a stable wire id from a channel type and direction.</summary>
public static class SyncChannelId
{
    public static ushort For(Type channelType,SycnDirection direction)
    {
        if(channelType==null)throw new ArgumentNullException(nameof(channelType));

        // 类型名和同步方向共同构成通道的协议身份。
        // 客户端和服务器都可以独立构造出相同的字符串。
        string key=$"{channelType.FullName}|{direction}";
        unchecked
        {
            // FNV-1a :字符串转成数字，但是无法还原，只是更分散
            // 使用固定的 32 位初始值。
            uint hash=2166136261u;
            for(int i=0;i<key.Length;i++)
            {
                // char 是 UTF-16 编码单元，转换为 uint 后参与按位异或。
                hash^=(uint)key[i];
                // 乘法溢出时只保留低 32 位，这是 FNV-1a 的设计行为。
                hash*=16777619u;
            }
            //uing转ushort
            // 将高 16 位折叠到低 16 位，避免直接截断导致高位信息丢失。
            ushort id=(ushort)(hash^(hash>>16));
            // 0 表示无效 ID，这里将它替换为 1。
            return id==0?(ushort)1:id;
        }
    }
}
