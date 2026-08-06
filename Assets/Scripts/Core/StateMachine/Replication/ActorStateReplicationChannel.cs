using System;

public sealed class ActorStateReplicationChannel
    : ActorReplicationChannel<ActorStateSnapshot>
{
    public const ushort Id=2;

    private readonly IReplicationProducer<ActorStateSnapshot> producer;
    private readonly IReplicationConsumer<ActorStateSnapshot> consumer;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;

    public ActorStateReplicationChannel(
        IReplicationProducer<ActorStateSnapshot> producer,
        IReplicationConsumer<ActorStateSnapshot> consumer)
    {
        this.producer=producer??
            throw new ArgumentNullException(nameof(producer));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out ActorStateSnapshot payload)
    {
        return producer.TryProduce(in context,out payload);
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in ActorStateSnapshot payload)
    {
        consumer.Receive(in context,in payload);
    }
}
