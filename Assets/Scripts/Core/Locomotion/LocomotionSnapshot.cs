using Unity.Netcode;

public struct LocomotionSnapshot : INetworkSerializable
{
    public LocomotionData Data;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Data.DesiredWorldMoveDirection);
        serializer.SerializeValue(ref Data.DesiredLocalMoveAngle);
        serializer.SerializeValue(ref Data.stateType);
    }
}
