using System;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class NetWorkPlayerController : InputSystem_Actions.IPlayerActions,IDisposable
{
    private readonly LocalInputData inputData=new();
    private InputSystem_Actions inputs;
    private InputButtons pressedButtons;

    public LocalInputData InputData => inputData;

    public void EnableInput()
    {
        if(inputs!=null)return;

        inputData.ClearInputIntents();
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

        inputData.ClearInputIntents();
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
            InputMove=inputData.InputMove,
            InputLook=inputData.InputLook,
            Held=GetHeldButtons(),
            Pressed=pressedButtons,
        };

        pressedButtons=InputButtons.None;
        return data;
    }

    private InputButtons GetHeldButtons()
    {
        InputButtons held=InputButtons.None;
        if(inputData.InputAttack)held|=InputButtons.InputAttack;
        if(inputData.InputAim)held|=InputButtons.InputAim;
        if(inputData.InputInteract)held|=InputButtons.InputInteract;
        if(inputData.InputCrouch)held|=InputButtons.InputCrouch;
        if(inputData.InputJump)held|=InputButtons.InputJump;
        if(inputData.InputPrevious)held|=InputButtons.InputPrevious;
        if(inputData.InputNext)held|=InputButtons.InputNext;
        if(inputData.InputSprint)held|=InputButtons.InputSprint;
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
        inputData.InputMove=context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        inputData.InputLook=context.ReadValue<Vector2>();
        inputData.LookIsPointerDelta=context.control?.device is Pointer;
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        inputData.InputAttack=ReadButton(context,InputButtons.InputAttack);
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        inputData.InputInteract=ReadButton(context,InputButtons.InputInteract);
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        inputData.InputCrouch=ReadButton(context,InputButtons.InputCrouch);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        inputData.InputJump=ReadButton(context,InputButtons.InputJump);
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        inputData.InputPrevious=ReadButton(context,InputButtons.InputPrevious);
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        inputData.InputNext=ReadButton(context,InputButtons.InputNext);
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        inputData.InputSprint=ReadButton(context,InputButtons.InputSprint);
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        inputData.InputAim=ReadButton(context,InputButtons.InputAim);
    }

}
