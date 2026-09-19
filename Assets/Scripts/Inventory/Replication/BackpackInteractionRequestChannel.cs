using Unity.Collections;
using Unity.Netcode;

public sealed class BackpackInteractionRequestChannel :
    ActorSycnChannel<BackpackInteractionRequestChannel.Request>
{
    public struct Request : INetworkSerializable
    {
        public int instanceId;
        public FixedString64Bytes optionId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref instanceId);
            serializer.SerializeValue(ref optionId);
        }
    }

    private bool dirty;
    private Request pending;

    public override SycnDirection direction => SycnDirection.OwnerToServer;
    public override SyncDataKind DataKind=>SyncDataKind.Command;
    public override SyncSchedule Schedule=>SyncSchedule.Queued;
    public override bool HasPendingData=>dirty;

    public BackpackInteractionRequestChannel(Actor actor) : base(actor)
    {
        Register();
    }

    public void RequestInteraction(int instanceId, string optionId)
    {
        pending = new Request
        {
            instanceId = instanceId,
            optionId = optionId
        };
        dirty = true;
        MarkDirty();
    }

    public override bool TryWrite(uint tick, FastBufferWriter writer)
    {
        if (!dirty)
        {
            return false;
        }

        writer.WriteNetworkSerializable(pending);
        dirty = false;
        return true;
    }

    public override bool TryApply(
        uint tick,
        FastBufferReader reader,
        int payloadEnd)
    {
        reader.ReadNetworkSerializable(out Request request);
        if (reader.Position != payloadEnd || !actor.IsServer)
        {
            return false;
        }

        return actor.inventorySystem.ExecuteBackpackInteraction(
            request.instanceId,
            request.optionId.ToString());
    }
}
