using System;

/// <summary>
/// 客户端共享的网络 Tick 时钟。
/// 负责把本地 Tick 映射到服务器 Tick，并计算延迟后的目标表现 Tick。
/// </summary>
public sealed class NetworkTickClock : IDisposable
{
    private readonly int presentationDelayTicks;
    private bool hasServerClock;
    private double serverTickOffset;

    /// <summary>最近收到的服务器数据包 Tick。</summary>
    public uint LatestReceivedServerTick{get;private set;}
    /// <summary>根据本地 Tick 和时钟偏移估算出的当前服务器时间。</summary>
    public double EstimatedServerTime{get;private set;}
    /// <summary>减去表现延迟后，当前应该采样的服务器时间。</summary>
    public double RequestedPresentationTime{get;private set;}
    /// <summary>当前客户端实际能够显示的服务器 Tick，不会超过最近收到的服务器 Tick。</summary>
    public uint DisplayedServerTick{get;private set;}
    /// <summary>表现层故意落后的 Tick 数。</summary>
    public int PresentationDelayTicks=>presentationDelayTicks;
    /// <summary>是否已经通过服务器数据包建立了服务器时钟关系。</summary>
    public bool HasServerClock=>hasServerClock;

    public NetworkTickClock(ActorSyncConfigSO config)
    {
        presentationDelayTicks=config!=null?config.PresentationDelayTicks:2;
    }

    /// <summary>根据本地 Tick 获取当前目标表现服务器 Tick。</summary>
    public uint GetPresentationTick(uint localTick)
    {
        double time=GetRequestedPresentationTime(localTick);
        return time<=0d?0u:(uint)time;
    }

    /// <summary>
    /// 获取实际可显示的服务器 Tick。
    /// 目标表现时间可能领先于网络上已经到达的数据，因此必须限制在最近收到的服务器 Tick 内。
    /// </summary>
    public uint GetDisplayedServerTick(uint localTick)
    {
        if(!hasServerClock)
            return 0;

        uint targetTick=GetPresentationTick(localTick);
        DisplayedServerTick=Math.Min(targetTick,LatestReceivedServerTick);
        return DisplayedServerTick;
    }

    /// <summary>记录服务器包到达时的服务器 Tick，并平滑本地与服务器的时钟偏移。</summary>
    public void ObserveServerPacket(uint serverTick,uint localReceiveTick)
    {
        if(!hasServerClock)
        {
            serverTickOffset=(double)serverTick-localReceiveTick;
            hasServerClock=true;
        }
        else
        {
            double observedOffset=(double)serverTick-localReceiveTick;
            serverTickOffset=serverTickOffset*0.9d+observedOffset*0.1d;
        }

        if(serverTick>=LatestReceivedServerTick)
            LatestReceivedServerTick=serverTick;
    }

    /// <summary>推进全局时钟，更新估算服务器时间和延迟后的目标表现时间。</summary>
    public void Advance(uint localTick)
    {
        EstimatedServerTime=hasServerClock
            ?localTick+serverTickOffset
            :localTick;
        RequestedPresentationTime=Math.Max(
            0d,
            EstimatedServerTime-presentationDelayTicks);

        if(hasServerClock&&LatestReceivedServerTick>0)
        {
            uint targetTick=RequestedPresentationTime<=0d
                ?0u
                :(uint)RequestedPresentationTime;
            DisplayedServerTick=Math.Min(targetTick,LatestReceivedServerTick);
        }
    }

    /// <summary>计算指定本地 Tick 对应的目标表现时间，不修改时钟状态。</summary>
    public double GetRequestedPresentationTime(uint localTick)
    {
        double estimated=hasServerClock
            ?localTick+serverTickOffset
            :localTick;
        return Math.Max(0d,estimated-presentationDelayTicks);
    }

    public void Dispose()
    {
        hasServerClock=false;
        serverTickOffset=0d;
        LatestReceivedServerTick=0;
        EstimatedServerTime=0d;
        RequestedPresentationTime=0d;
        DisplayedServerTick=0;
    }
}
