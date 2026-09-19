using Unity.Netcode;

public struct AimModeStateSnapshot : INetworkSerializable
{
    public bool IsAiming;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref IsAiming);
    }
}
