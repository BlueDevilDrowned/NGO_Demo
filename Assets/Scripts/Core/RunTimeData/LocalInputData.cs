using JetBrains.Annotations;
using UnityEngine;

public class LocalInputData
{
    public Vector2 InputMove;
    public Vector2 InputLook;
    public bool LookIsPointerDelta;
    public bool InputAttack;
    public bool InputAim;
    public bool InputInteract;
    public bool InputCrouch;
    public bool InputJump;
    public bool InputPrevious;
    public bool InputNext;
    public bool InputSprint;
    public void ClearInputIntents()
    {
        InputMove=Vector2.zero;
        InputLook=Vector2.zero;
        LookIsPointerDelta=false;
        InputAttack=false;
        InputAim=false;
        InputInteract=false;
        InputCrouch=false;
        InputJump=false;
        InputPrevious=false;
        InputNext=false;
        InputSprint=false;
    }
}
