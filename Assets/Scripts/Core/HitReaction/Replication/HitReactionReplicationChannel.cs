using System;

public sealed class HitReactionReplicationChannel
    : ActorReplicationChannel<HitReactionSnapshot>
{
    public const ushort Id=8;

    private readonly IReplicationProducer<HitReactionSnapshot> producer;
    private readonly IReplicationConsumer<HitReactionSnapshot> consumer;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;

    public HitReactionReplicationChannel(
        IReplicationProducer<HitReactionSnapshot> producer,
        IReplicationConsumer<HitReactionSnapshot> consumer)
    {
        this.producer=producer??throw new ArgumentNullException(nameof(producer));
        this.consumer=consumer??throw new ArgumentNullException(nameof(consumer));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out HitReactionSnapshot payload)
    {
        return producer.TryProduce(in context,out payload);
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in HitReactionSnapshot payload)
    {
        consumer.Receive(in context,in payload);
    }
}
