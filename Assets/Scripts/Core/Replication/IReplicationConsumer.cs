public interface IReplicationConsumer<TPayload>
{
    void Receive(
        in ActorReplicationContext context,
        in TPayload payload);
}
