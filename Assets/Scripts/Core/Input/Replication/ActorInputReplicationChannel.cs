using System;

public sealed class ActorInputReplicationChannel
    : ActorReplicationChannel<ActorInputCommand>
{
    public const ushort Id=1;

    private readonly IReplicationProducer<ActorInputCommand> producer;
    private readonly IReplicationConsumer<ActorInputCommand> consumer;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>ActorReplicationDirection.OwnerToServer;

    public ActorInputReplicationChannel(
        IReplicationProducer<ActorInputCommand> producer,
        IReplicationConsumer<ActorInputCommand> consumer)
    {
        this.producer=producer??
            throw new ArgumentNullException(nameof(producer));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out ActorInputCommand payload)
    {
        return producer.TryProduce(in context,out payload);
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in ActorInputCommand payload)
    {
        consumer.Receive(in context,in payload);
    }
}
