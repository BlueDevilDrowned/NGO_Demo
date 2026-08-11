using System;

public sealed class HitReactionSynchronizer
{
    private readonly HitReactionSystem hitReaction;
    private readonly HitReactionSnapshotConsumer consumer;

    public HitReactionSynchronizer(
        HitReactionSystem hitReaction,
        HitReactionSnapshotConsumer consumer)
    {
        this.hitReaction=hitReaction??
            throw new ArgumentNullException(nameof(hitReaction));
        this.consumer=consumer??throw new ArgumentNullException(nameof(consumer));
    }

    public void ApplyPendingSnapshots()
    {
        while(consumer.TryConsume(out HitReactionSnapshot snapshot))
        {
            int count=Math.Min(
                (int)snapshot.EventCount,
                HitReactionSnapshot.MaxEvents);
            for(int i=0;i<count;i++)
            {
                HitReactionEvent reaction=snapshot.GetEvent(i);
                hitReaction.ApplyAuthoritativeEvent(in reaction);
            }
        }
    }
}
