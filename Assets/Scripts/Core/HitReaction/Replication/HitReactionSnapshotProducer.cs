using System;

public sealed class HitReactionSnapshotProducer
    : IReplicationProducer<HitReactionSnapshot>
{
    private readonly HitReactionSystem hitReaction;

    public HitReactionSnapshotProducer(HitReactionSystem hitReaction)
    {
        this.hitReaction=hitReaction??
            throw new ArgumentNullException(nameof(hitReaction));
    }

    public bool TryProduce(
        in ActorReplicationContext context,
        out HitReactionSnapshot snapshot)
    {
        snapshot=default;
        if(!context.IsServer)return false;

        snapshot.Tick=context.Tick;
        hitReaction.CopyRecentEvents(ref snapshot);
        return true;
    }
}
