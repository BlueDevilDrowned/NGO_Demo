/// <summary>
/// 当前运行场景共享的同步运行时状态。
/// 一个客户端进程只维护一套 NetworkTickClock，所有 Actor 共用它。
/// </summary>
public static class ActorSyncRuntime
{
    private static NetworkTickClock clock;

    public static NetworkTickClock GetOrCreate(ActorSyncConfigSO config)
    {
        return clock??=new NetworkTickClock(config);
    }

    public static void Initialize(ActorSyncConfigSO config)
    {
        if(clock==null)
            clock=new NetworkTickClock(config);
    }

    public static void Reset()
    {
        clock?.Dispose();
        clock=null;
    }
}
