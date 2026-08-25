using Unity.Netcode;
/// <summary>
/// 状态同步快照，同步权威target
/// </summary>
public struct AimStateSnapshot : INetworkSerializable
{
    public AimData Data;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Data.IsAiming);
        serializer.SerializeValue(ref Data.TargetPosition);
    }
}
