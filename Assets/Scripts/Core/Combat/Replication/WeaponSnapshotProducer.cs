using System;

public sealed class WeaponSnapshotProducer
    : IReplicationProducer<WeaponSnapshot>
{
    private readonly WeaponSystem weapon;

    public WeaponSnapshotProducer(WeaponSystem weapon)
    {
        this.weapon=weapon??throw new ArgumentNullException(nameof(weapon));
    }

    public bool TryProduce(
        in ActorReplicationContext context,
        out WeaponSnapshot snapshot)
    {
        snapshot=default;
        if(!context.IsServer)return false;

        snapshot=new WeaponSnapshot
        {
            Tick=context.Tick,
            EquippedWeaponId=weapon.CurrentWeaponId,
        };
        weapon.CopyRecentEvents(ref snapshot);
        return true;
    }
}
