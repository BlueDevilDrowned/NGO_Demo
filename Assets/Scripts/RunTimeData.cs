using UnityEngine;

public class RunTimeData
{
    [Header("Input")]
    public Vector2 InputMove;
    public Vector2 InputLook;
    public bool InputAttack;
    public bool InputInteract;
    public bool InputCrouch;
    public bool InputJump;
    public bool InputPrevious;
    public bool InputNext;
    public bool InputSprint;

    public void ClearInputIntents()
    {
        InputMove = Vector2.zero;
        InputLook = Vector2.zero;
        InputAttack = false;
        InputInteract = false;
        InputCrouch = false;
        InputJump = false;
        InputPrevious = false;
        InputNext = false;
        InputSprint = false;
    }
}
