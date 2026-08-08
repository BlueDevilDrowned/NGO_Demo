using Unity.Netcode;

public struct AimSnapshot : INetworkSerializable
{
    public uint Tick;
    public AimData Data;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Data.ViewYaw);
        serializer.SerializeValue(ref Data.ViewPitch);
        serializer.SerializeValue(ref Data.TargetPosition);
    }
}
