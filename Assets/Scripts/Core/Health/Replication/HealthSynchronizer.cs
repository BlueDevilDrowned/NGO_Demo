using System;

public sealed class HealthSynchronizer
{
    private readonly HealthSystem health;
    private readonly HealthSnapshotConsumer consumer;

    public HealthSynchronizer(
        HealthSystem health,
        HealthSnapshotConsumer consumer)
    {
        this.health=health??throw new ArgumentNullException(nameof(health));
        this.consumer=consumer??throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingSnapshot()
    {
        if(!consumer.TryConsume(out HealthSnapshot snapshot))return;

        health.ApplyAuthoritativeSnapshot(
            snapshot.CurrentHealth,
            snapshot.MaxHealth);
    }
}
