public interface IReplicationProducer<TPayload>
{
    bool TryProduce(
        in ActorReplicationContext context,
        out TPayload payload);
}
