//负责整合接口，并注册进同步系统，tick更新
public abstract class ActorSycnChannel<T, G> : IReplicationAdapter<T, G>
{
    public void Apply(G Simulation)
    {
        
    }

    public bool TryCapture(T Intent)
    {
        return true;
    }

}