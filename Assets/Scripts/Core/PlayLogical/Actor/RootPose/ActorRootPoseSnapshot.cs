using Unity.Netcode;

public struct ActorRootPoseSnapshot:INetworkSerializable
{
    public float Yaw;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T:IReaderWriter
    {
        serializer.SerializeValue(ref Yaw);
    }
}

public static class ActorRootPoseSnapshotUtility
{
    public static bool IsValid(in ActorRootPoseSnapshot snapshot)
    {
        return ActorCameraDataUtility.IsFinite(snapshot.Yaw);
    }
}
