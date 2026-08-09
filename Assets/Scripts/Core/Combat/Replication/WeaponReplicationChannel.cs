using System;

public sealed class WeaponReplicationChannel
    : ActorReplicationChannel<WeaponSnapshot>
{
    public const ushort Id=6;

    private readonly IReplicationProducer<WeaponSnapshot> producer;
    private readonly IReplicationConsumer<WeaponSnapshot> consumer;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;

    public WeaponReplicationChannel(
        IReplicationProducer<WeaponSnapshot> producer,
        IReplicationConsumer<WeaponSnapshot> consumer)
    {
        this.producer=producer??
            throw new ArgumentNullException(nameof(producer));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    protected override bool TryWrite(
        in ActorReplicationContext context,
        out WeaponSnapshot payload)
    {
        return producer.TryProduce(in context,out payload);
    }

    protected override void Apply(
        in ActorReplicationContext context,
        in WeaponSnapshot payload)
    {
        consumer.Receive(in context,in payload);
    }
}
