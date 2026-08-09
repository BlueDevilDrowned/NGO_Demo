using System;

public sealed class UpperBodyStateReplicationChannel
    : ActorReplicationChannel<UpperBodyStateSnapshot>
{
    public const ushort Id=5;

    private readonly IReplicationProducer<UpperBodyStateSnapshot> producer;
    private readonly IReplicationConsumer<UpperBodyStateSnapshot> consumer;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;

    public UpperBodyStateReplicationChannel(
        IReplicationProducer<UpperBodyStateSnapshot> producer,
        IReplicationConsumer<UpperBodyStateSnapshot> consumer)
    {
        this.producer=producer??
            throw new ArgumentNullException(nameof(producer));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out UpperBodyStateSnapshot payload)
    {
        return producer.TryProduce(in context,out payload);
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in UpperBodyStateSnapshot payload)
    {
        consumer.Receive(in context,in payload);
    }
}
