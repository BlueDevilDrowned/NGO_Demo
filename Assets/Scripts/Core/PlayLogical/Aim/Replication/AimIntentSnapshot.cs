using Unity.Netcode;

public struct AimIntentSnapshot : INetworkSerializable
{
    public bool IsAiming;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref IsAiming);
    }
}
