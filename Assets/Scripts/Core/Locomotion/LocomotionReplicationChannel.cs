using System;

public sealed class LocomotionReplicationChannel
    : ActorReplicationChannel<LocomotionSnapshot>
{
    public const ushort Id=3;
    //身缠消费系统
    private readonly IReplicationProducer<LocomotionSnapshot> producer;
    private readonly IReplicationConsumer<LocomotionSnapshot> consumer;

    public override ushort ChannelId=>Id;
    public override ActorReplicationDirection Direction=>
        ActorReplicationDirection.ServerToClients;
    //创建
    public LocomotionReplicationChannel(IReplicationProducer<LocomotionSnapshot> producer,IReplicationConsumer<LocomotionSnapshot> consumer)
    {
        this.producer=producer??
            throw new ArgumentNullException(nameof(producer));
        this.consumer=consumer??
            throw new ArgumentNullException(nameof(consumer));
    }

    protected override bool TryWrite( in ActorReplicationContext context,out LocomotionSnapshot payload)
    {
        return producer.TryProduce(in context,out payload);
    }

    protected override void Apply(in ActorReplicationContext context,in LocomotionSnapshot payload)
    {
        consumer.Receive(in context,in payload);
    }
}
