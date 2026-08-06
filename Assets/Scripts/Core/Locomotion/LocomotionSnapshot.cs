using Unity.Netcode;

public struct LocomotionSnapshot : INetworkSerializable
{
    public uint Tick;
    public LocomotionData Data;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref Data.DesiredWorldMoveDirection);
        serializer.SerializeValue(ref Data.DesiredLocalMoveAngle);
    }
}
