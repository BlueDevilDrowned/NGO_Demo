using System;

public sealed class WeaponSynchronizer
{
    private readonly WeaponSystem weapon;
    private readonly WeaponEquipmentSystem equipment;
    private readonly WeaponSnapshotConsumer consumer;

    public WeaponSynchronizer(
        WeaponSystem weapon,
        WeaponEquipmentSystem equipment,
        WeaponSnapshotConsumer consumer)
    {
        this.weapon=weapon??throw new ArgumentNullException(nameof(weapon));
        this.equipment=equipment??
            throw new ArgumentNullException(nameof(equipment));
        this.consumer=consumer??throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingSnapshot()
    {
        while(consumer.TryConsume(out WeaponSnapshot snapshot))
        {
            if(snapshot.EquippedWeaponId==0)
                equipment.Unequip();
            else
                equipment.Equip(snapshot.EquippedWeaponId);
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
