using Unity.Netcode;

public struct HealthSnapshot : INetworkSerializable
{
    public uint Tick;
    public float CurrentHealth;
    public float MaxHealth;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref CurrentHealth);
        serializer.SerializeValue(ref MaxHealth);
    }
}
