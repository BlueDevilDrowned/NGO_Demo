using System;
using Unity.Netcode;
using UnityEngine;
[Serializable]
public struct ActorInputCommand : INetworkSerializable
{
    // BufferSerializer 会根据 T 是 Reader 还是 Writer，决定从字段读取还是向字段写入。
    // ref 很关键：写入网络时序列化器读取字段；读取网络时序列化器要能修改字段。
    // 两端必须按完全相同的顺序调用 SerializeValue，这个顺序就是二进制协议的一部分。
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref InputMove);
        serializer.SerializeValue(ref InputLook);
        serializer.SerializeValue(ref Held);
        serializer.SerializeValue(ref Pressed);
        serializer.SerializeValue(ref ViewYaw);
    }
    public uint Tick;
    public Vector2 InputMove;
    public Vector2 InputLook;
    //行否处于按下状态
    public InputButtons Held;
    //刚刚按下
    public InputButtons Pressed;
    //相机角
    public float ViewYaw;
}
[Flags]
public enum InputButtons:ushort
{
    None=0,
    InputAttack=1<<0,
    InputInteract=1<<1,
    InputCrouch=1<<2,
    InputJump=1<<3,
    InputPrevious=1<<4,
    InputNext=1<<5,
    InputSprint=1<<6,
}
