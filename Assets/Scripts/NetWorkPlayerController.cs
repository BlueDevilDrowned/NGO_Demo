using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetWorkPlayerController : NetworkBehaviour, InputSystem_Actions.IPlayerActions
{
    [SerializeField]
    public Actor actor;

    private BlackBorad blackBorad;
    private InputSystem_Actions inputs;

    private void Awake()
    {
        if (actor == null)
        {
            actor = GetComponent<Actor>();
        }

        blackBorad = actor != null && actor.blackBorad != null
            ? actor.blackBorad
            : GetComponent<BlackBorad>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsLocalPlayer)
        {
            return;
        }

        if (blackBorad == null)
        {
            Debug.LogError("NetWorkPlayerController requires a BlackBorad reference.", this);
            return;
        }

        blackBorad.ClearInputIntents();
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
        blackBorad?.ClearInputIntents();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (blackBorad != null)
        {
            blackBorad.InputMove = context.ReadValue<Vector2>();
        }
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (blackBorad != null)
        {
            blackBorad.InputLook = context.ReadValue<Vector2>();
        }
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (blackBorad != null)
        {
            blackBorad.InputAttack = context.ReadValueAsButton();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (blackBorad == null)
        {
            return;
        }

        if (context.performed)
        {
            blackBorad.InputInteract = true;
        }
        else if (context.canceled)
        {
            blackBorad.InputInteract = false;
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (blackBorad != null)
        {
            blackBorad.InputCrouch = context.ReadValueAsButton();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (blackBorad != null)
        {
            blackBorad.InputJump = context.ReadValueAsButton();
        }
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        if (blackBorad != null)
        {
            blackBorad.InputPrevious = context.ReadValueAsButton();
        }
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        if (blackBorad != null)
        {
            blackBorad.InputNext = context.ReadValueAsButton();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (blackBorad != null)
        {
            blackBorad.InputSprint = context.ReadValueAsButton();
        }
    }
}
