using System;

public sealed class HealthSnapshotProducer
    : IReplicationProducer<HealthSnapshot>
{
    private readonly HealthSystem health;

    public HealthSnapshotProducer(HealthSystem health)
    {
        this.health=health??throw new ArgumentNullException(nameof(health));
    }

    public bool TryProduce(
        in ActorReplicationContext context,
        out HealthSnapshot snapshot)
    {
        snapshot=default;
        if(!context.IsServer)return false;

        snapshot=new HealthSnapshot
        {
            Tick=context.Tick,
            CurrentHealth=health.CurrentHealth,
            MaxHealth=health.MaxHealth,
        };
        return true;
    }
}
