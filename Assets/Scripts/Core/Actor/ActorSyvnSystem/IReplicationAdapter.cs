public interface IReplicationAdapter<T,G>
{
    public bool TryCapture(T Intent); 
    public void Apply(G Simulation);
}