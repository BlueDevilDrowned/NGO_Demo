using Unity.Netcode;

public struct ActorPerspectiveIntentSnapshot : INetworkSerializable
{
    public CameraPerspectiveMode Mode;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Mode);
    }
}

public struct ActorPerspectiveStateSnapshot : INetworkSerializable
{
    public CameraPerspectiveMode Mode;
    public uint ProcessedInputTick;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Mode);
        serializer.SerializeValue(ref ProcessedInputTick);
    }
}

public struct ActorPerspectiveRequest
{
    public CameraPerspectiveMode Mode;
    public uint InputTick;
}

public static class ActorPerspectiveSnapshotUtility
{
    public static bool IsValid(CameraPerspectiveMode mode)
    {
        return mode is CameraPerspectiveMode.ThirdPerson or
            CameraPerspectiveMode.FirstPerson;
    }
}
