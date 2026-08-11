using System;

public sealed class HealthReplicationChannel
    : ActorReplicationChannel<HealthSnapshot>
{
    public const ushort Id=7;

    private readonly IReplicationProducer<HealthSnapshot> producer;
    private readonly IReplicationConsumer<HealthSnapshot> consumer;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;

    public HealthReplicationChannel(
        IReplicationProducer<HealthSnapshot> producer,
        IReplicationConsumer<HealthSnapshot> consumer)
    {
        this.producer=producer??
            throw new ArgumentNullException(nameof(producer));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out HealthSnapshot payload)
    {
        return producer.TryProduce(in context,out payload);
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in HealthSnapshot payload)
    {
        consumer.Receive(in context,in payload);
    }
}
