using Unity.Netcode;
using InventorySolver;

public sealed class InventoryPlacementRequestChannel : ActorSycnChannel<InventoryPlacementRequestChannel.Request>
{
    public struct Request : INetworkSerializable
    {
        public int instanceId;
        public int regionIndex;
        public int x;
        public int y;
        public ERotation rotation;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer)
            where T : IReaderWriter
        {
            serializer.SerializeValue(ref instanceId);
            serializer.SerializeValue(ref regionIndex);
            serializer.SerializeValue(ref x);
            serializer.SerializeValue(ref y);
            serializer.SerializeValue(ref rotation);
        }
    }

    private bool dirty;
    private Request pending;
    private uint lastAppliedTick;
    private bool hasAppliedTick;

    public override SycnDirection direction => SycnDirection.OwnerToServer;

    public InventoryPlacementRequestChannel(Actor actor) : base(actor)
    {
        Register();
    }

    public void RequestPlacement(
        int instanceId,
        int regionIndex,
        int x,
        int y,
        ERotation rotation)
    {
        pending = new Request
        {
            instanceId = instanceId,
            regionIndex = regionIndex,
            x = x,
            y = y,
            rotation = rotation
        };
        dirty = true;
    }

    public override bool TryWrite(uint tick, FastBufferWriter writer)
    {
        if (!dirty)
            return false;

        writer.WriteNetworkSerializable(pending);
        dirty = false;
        return true;
    }

    public override bool TryApply(uint tick, FastBufferReader reader, int payloadEnd)
    {
        reader.ReadNetworkSerializable(out Request request);
        if (reader.Position != payloadEnd || !actor.IsServer || hasAppliedTick && tick <= lastAppliedTick)
            return false;

        if (!System.Enum.IsDefined(typeof(ERotation), request.rotation))
            return false;

        var placement = new Placement(
            request.regionIndex,
            new Cell(request.x, request.y),
            request.rotation);
        bool applied = actor.inventorySystem.TryCommitPlacement(request.instanceId, placement);
        if (applied)
        {
            lastAppliedTick = tick;
            hasAppliedTick = true;
        }
        return applied;
    }
}
