using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public struct ActorStateSnapshot : INetworkSerializable
{
    //客户端状态机有关动画设置所需的参数同步
    public ActorStateType StateType;
    public uint StateEnterTick;
    public ActorInputCommand input;
    public ActorStateBlackboard blackboard;
    // input 和 blackboard 自己也实现了 INetworkSerializable，NGO 会继续进入它们的
    // NetworkSerialize；因此嵌套结构仍然能被同一个 Writer/Reader 处理。
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref StateType);
        serializer.SerializeValue(ref StateEnterTick);
        serializer.SerializeValue(ref input);
        serializer.SerializeValue(ref blackboard);
    }
}
