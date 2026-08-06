using Unity.Netcode;

public struct ActorStateSnapshot : INetworkSerializable
{
    //客户端状态机有关动画设置所需的参数同步
    public uint Tick;
    public ActorStateType StateType;
    public uint StateEnterTick;
    public ActorStateBlackboard blackboard;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref StateType);
        serializer.SerializeValue(ref StateEnterTick);
        serializer.SerializeValue(ref blackboard.StartFootIsL);
        serializer.SerializeValue(ref blackboard.LastMoveState);
        serializer.SerializeValue(ref blackboard.Parameter);
    }
}
