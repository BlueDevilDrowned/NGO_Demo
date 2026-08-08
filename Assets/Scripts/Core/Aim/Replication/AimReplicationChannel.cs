using System;

public sealed class AimReplicationChannel
    : ActorReplicationChannel<AimSnapshot>
{
    public const ushort Id=4;

    private readonly IReplicationProducer<AimSnapshot> producer;
    private readonly IReplicationConsumer<AimSnapshot> consumer;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;

    public AimReplicationChannel(
        IReplicationProducer<AimSnapshot> producer,
        IReplicationConsumer<AimSnapshot> consumer)
    {
        this.producer=producer??
            throw new ArgumentNullException(nameof(producer));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out AimSnapshot payload)
    {
        return producer.TryProduce(in context,out payload);
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in AimSnapshot payload)
    {
        consumer.Receive(in context,in payload);
    }
}
