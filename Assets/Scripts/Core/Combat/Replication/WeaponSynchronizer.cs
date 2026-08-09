using System;

public sealed class WeaponSynchronizer
{
    private readonly WeaponSystem weapon;
    private readonly WeaponSnapshotConsumer consumer;

    public WeaponSynchronizer(
        WeaponSystem weapon,
        WeaponSnapshotConsumer consumer)
    {
        this.weapon=weapon??throw new ArgumentNullException(nameof(weapon));
        this.consumer=consumer??throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingSnapshot()
    {
        while(consumer.TryConsume(out WeaponSnapshot snapshot))
        {
            int eventCount=Math.Min(
                (int)snapshot.EventCount,
                WeaponSnapshot.MaxEvents);
            for(int i=0;i<eventCount;i++)
            {
                ShotData shotEvent=snapshot.GetEvent(i);
                weapon.ApplyAuthoritativeShot(in shotEvent);
            }
        }
    }
}
