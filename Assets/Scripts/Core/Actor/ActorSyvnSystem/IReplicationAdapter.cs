public interface IReplicationAdapter<T>
{
    public IReplicationAdapter<T> Initialize()
    {
        return this;
    }
    public bool TryWrite(T Intent); 
    public bool TryApply(T Intent);
}