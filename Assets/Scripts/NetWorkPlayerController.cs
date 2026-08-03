using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetWorkPlayerController : NetworkBehaviour, InputSystem_Actions.IPlayerActions
{
    [SerializeField]
    public Actor actor;

    private RunTimeData runTimeData=>actor.runTimeData;
    private InputSystem_Actions inputs;

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<Actor>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            return;
        }

        if (runTimeData == null)
        {
            Debug.LogError("NetWorkPlayerController requires a BlackBorad reference.", this);
            return;
        }

        runTimeData.ClearInputIntents();
        inputs = new InputSystem_Actions();
        inputs.Player.AddCallbacks(this);
        inputs.Player.Enable();
    }

    public override void OnNetworkDespawn()
    {
        ReleaseInput();
    }

    public override void OnDestroy()
    {
        ReleaseInput();
        base.OnDestroy();
    }

    private void ReleaseInput()
    {
        if (inputs == null)
        {
            return;
        }

        inputs.Player.RemoveCallbacks(this);
        inputs.Player.Disable();
        inputs.Dispose();
        inputs = null;
        runTimeData?.ClearInputIntents();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (runTimeData != null)
        {
            runTimeData.InputMove = context.ReadValue<Vector2>();
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (runTimeData != null)
        {
            runTimeData.InputLook = context.ReadValue<Vector2>();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (runTimeData != null)
        {
            runTimeData.InputAttack = context.ReadValueAsButton();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (runTimeData == null)
        {
            return;
        }

        if (context.performed)
        {
            runTimeData.InputInteract = true;
        }
        else if (context.canceled)
        {
            runTimeData.InputInteract = false;
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (runTimeData != null)
        {
            runTimeData.InputCrouch = context.ReadValueAsButton();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (runTimeData != null)
        {
            runTimeData.InputJump = context.ReadValueAsButton();
        }
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        if (runTimeData != null)
        {
            runTimeData.InputPrevious = context.ReadValueAsButton();
        }
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        if (runTimeData != null)
        {
            runTimeData.InputNext = context.ReadValueAsButton();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (runTimeData != null)
        {
            runTimeData.InputSprint = context.ReadValueAsButton();
        }
    }
}
