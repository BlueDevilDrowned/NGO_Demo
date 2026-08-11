public partial class Actor
{
    internal void ReceiveProjectileHit(in ProjectileHitResult hit)
    {
        if(!IsServer)return;

        health?.ReceiveProjectileHit(in hit);
        hitReaction?.ReceiveProjectileHit(in hit);
    }
}
