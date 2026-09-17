using System;
using UnityEngine;

public struct ActorInputData
{
    public Vector2 InputMove;
    public Vector2 InputLook;
    public Vector2 InputScroll;
    public ulong InteractionTarget;
    public Unity.Collections.FixedString64Bytes InteractionOption;
    public uint ClientShotId;
    public InputButtons Held;
    public InputButtons Pressed;

    public readonly bool WasPressed(InputButtons button)
    {
        return (Pressed&button)==button;
    }

    public readonly bool IsHeld(InputButtons button)
    {
        return (Held&button)==button;
    }

    public readonly bool WasAnyPressed(InputButtons buttons)
    {
        return (Pressed&buttons)!=0;
    }

    public readonly bool IsAnyHeld(InputButtons buttons)
    {
        return (Held&buttons)!=0;
    }
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
    InputAim=1<<7,
    InputChange=1<<8,
    InputDrop=1<<9,
    InputBag=1<<10,
    InputCancel=1<<11,
    InputUnlockMouse=1<<12,
}
