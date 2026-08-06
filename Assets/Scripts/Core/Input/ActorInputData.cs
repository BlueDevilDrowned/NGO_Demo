using System;
using UnityEngine;

public struct ActorInputData
{
    public Vector2 InputMove;
    public Vector2 InputLook;
    public InputButtons Held;
    public InputButtons Pressed;
    public float ViewYaw;
}

[Flags]
public enum InputButtons : ushort
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
