using System;
using System.Collections.Generic;

/// <summary>
/// 单个 Actor 的连续同步历史仓库。
/// 不负责估算服务器时间，只按调用方提供的目标 Tick 保存和采样各 Channel 数据。
/// </summary>
public sealed class ActorSyncHistory : IDisposable
{
    private interface IBuffer
    {
        void Clear();
    }

    private sealed class Buffer<T> : IBuffer
    {
        private readonly int capacity;
        private readonly SortedDictionary<uint,T> values=new();
        private bool seeded;

        public Buffer(int capacity)
        {
            this.capacity=Math.Max(2,capacity);
        }

        public void Push(uint tick,in T value,int seedDelay)
        {
            if(!seeded)
            {
                uint first=tick>=(uint)seedDelay?tick-(uint)seedDelay:0;
                for(uint seed=first;;seed++)
                {
                    values[seed]=value;
                    if(seed==tick)break;
                }
                seeded=true;
            }
            else
            {
                values[tick]=value;
            }

            while(values.Count>capacity)
            {
                uint oldest=uint.MaxValue;
                foreach(uint key in values.Keys)
                {
                    oldest=key;
                    break;
                }
                values.Remove(oldest);
            }
        }

        public bool TryGet(uint tick,out T value,out uint sourceTick)
        {
            sourceTick=0;
            if(values.TryGetValue(tick,out value))
            {
                sourceTick=tick;
                return true;
            }

            if(values.Count==0)
            {
                value=default;
                return false;
            }

            uint selected=0;
            bool hasSelected=false;
            foreach(uint key in values.Keys)
            {
                if(key>tick)break;
                selected=key;
                hasSelected=true;
            }

            if(!hasSelected)
            {
                foreach(uint key in values.Keys)
                {
                    selected=key;
                    break;
                }
            }

            sourceTick=selected;
            return values.TryGetValue(selected,out value);
        }

        public void Clear()
        {
            values.Clear();
            seeded=false;
        }
    }

    private readonly Dictionary<ushort,IBuffer> buffers=new();
    private readonly int capacityTicks;
    private readonly int seedDelayTicks;

    public ActorSyncHistory(ActorSyncConfigSO config)
    {
        capacityTicks=config!=null?config.BufferCapacityTicks:36;
        seedDelayTicks=config!=null?config.PresentationDelayTicks:2;
    }

    /// <summary>把 Channel 在指定服务器 Tick 的数据写入历史仓库。</summary>
    public void Push<T>(IActorSycnChannel channel,uint tick,in T value)
    {
        if(channel==null)throw new ArgumentNullException(nameof(channel));

        ushort key=channel.ChannelId;
        if(!buffers.TryGetValue(key,out IBuffer raw))
        {
            raw=new Buffer<T>(capacityTicks);
            buffers.Add(key,raw);
        }

        if(raw is not Buffer<T> buffer)
            throw new InvalidOperationException($"Channel {channel.ChannelId} changed timeline data type.");

        buffer.Push(tick,in value,seedDelayTicks);
    }

    /// <summary>按目标服务器 Tick 采样，并返回实际命中的源 Tick。</summary>
    public bool TrySample<T>(
        IActorSycnChannel channel,
        uint tick,
        out T value,
        out uint sourceTick)
    {
        value=default;
        sourceTick=0;
        return channel!=null&&
               buffers.TryGetValue(channel.ChannelId,out IBuffer raw)&&
               raw is Buffer<T> buffer&&
               buffer.TryGet(tick,out value,out sourceTick);
    }

    /// <summary>移除 Channel 对应的历史数据。</summary>
    public void Unregister(IActorSycnChannel channel)
    {
        if(channel!=null)
            buffers.Remove(channel.ChannelId);
    }

    public void Dispose()
    {
        foreach(IBuffer buffer in buffers.Values)
            buffer.Clear();
        buffers.Clear();
    }
}
