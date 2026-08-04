using System;
using Unity.Netcode;
using UnityEngine;
[Serializable]
public struct ActorInputCommand : INetworkSerializable
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Tick);
        serializer.SerializeValue(ref InputMove);
        serializer.SerializeValue(ref InputLook);
        serializer.SerializeValue(ref Held);
        serializer.SerializeValue(ref Pressed);
    }
    public uint Tick;
    public Vector2 InputMove;
    public Vector2 InputLook;
    //行否处于按下状态
    public InputButtons Held;
    //刚刚按下
    public InputButtons Pressed;
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