using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class NetWorkPlayerController : InputSystem_Actions.IPlayerActions,IDisposable
{
    private readonly LocalInputState input=new();
    private InputSystem_Actions inputs;
    private InputButtons pressedButtons;

    public LocalInputState Input => input;

    public void EnableInput()
    {
        if(inputs!=null)return;
        //清理意图
        input.Clear();
        pressedButtons=InputButtons.None;

        inputs=new InputSystem_Actions();
        inputs.Player.AddCallbacks(this);
        inputs.Player.Enable();
    }

    public void DisableInput()
    {
        if(inputs!=null)
        {
            inputs.Player.RemoveCallbacks(this);
            inputs.Player.Disable();
            inputs.Dispose();
            inputs=null;
        }

        input.Clear();
        pressedButtons=InputButtons.None;
    }

    public void Dispose()
    {
        DisableInput();
    }

    public ActorInputData BuildInputData()
    {
        ActorInputData data=new()
        {
            InputMove=input.InputMove,
            InputLook=input.InputLook,
            Held=GetHeldButtons(),
            Pressed=pressedButtons,
        };

        pressedButtons=InputButtons.None;
        return data;
    }

    private InputButtons GetHeldButtons()
    {
        InputButtons held=InputButtons.None;
        if(input.InputAttack)held|=InputButtons.InputAttack;
        if(input.InputAim)held|=InputButtons.InputAim;
        if(input.InputInteract)held|=InputButtons.InputInteract;
        if(input.InputCrouch)held|=InputButtons.InputCrouch;
        if(input.InputJump)held|=InputButtons.InputJump;
        if(input.InputPrevious)held|=InputButtons.InputPrevious;
        if(input.InputNext)held|=InputButtons.InputNext;
        if(input.InputSprint)held|=InputButtons.InputSprint;
        return held;
    }

    private bool ReadButton(InputAction.CallbackContext context,InputButtons button)
    {
        if(context.performed)
        {
            pressedButtons|=button;
        }

        return context.ReadValueAsButton();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        input.InputMove=context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        input.InputLook=context.ReadValue<Vector2>();
        input.LookIsPointerDelta=context.control?.device is Pointer;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        input.InputAttack=ReadButton(context,InputButtons.InputAttack);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        input.InputInteract=ReadButton(context,InputButtons.InputInteract);
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        input.InputCrouch=ReadButton(context,InputButtons.InputCrouch);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        input.InputJump=ReadButton(context,InputButtons.InputJump);
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        input.InputPrevious=ReadButton(context,InputButtons.InputPrevious);
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        input.InputNext=ReadButton(context,InputButtons.InputNext);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        input.InputSprint=ReadButton(context,InputButtons.InputSprint);
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        input.InputAim=ReadButton(context,InputButtons.InputAim);
    }

}
