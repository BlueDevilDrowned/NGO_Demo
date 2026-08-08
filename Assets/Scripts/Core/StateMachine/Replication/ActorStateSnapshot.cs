using Unity.Netcode;

public struct ActorStateSnapshot : INetworkSerializable
{
    public uint Tick;
    public ActorStateType StateType;
    public ActorMode Mode;
    public uint StateEnterTick;
    public ActorStateBlackboard blackboard;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref StateType);
        serializer.SerializeValue(ref Mode);
        serializer.SerializeValue(ref StateEnterTick);
        serializer.SerializeValue(ref blackboard.StartFootIsL);
        serializer.SerializeValue(ref blackboard.LastMoveState);
        serializer.SerializeValue(ref blackboard.Parameter);
        serializer.SerializeValue(ref blackboard.ImpactSpeed);
    }
}
