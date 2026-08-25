using Unity.Netcode;

public struct WeaponEquipmentSnapshot : INetworkSerializable
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ProcessedInputTick);
        serializer.SerializeValue(ref data.id);
    }

    public uint ProcessedInputTick;
    public WeaponEquipmentData data;
}
