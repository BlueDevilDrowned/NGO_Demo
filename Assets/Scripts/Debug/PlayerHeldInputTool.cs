using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerHeldInputTool:MonoBehaviour
{
    [SerializeField]private bool holdInput;
    [SerializeField]private InputButtons button=InputButtons.InputAim;

    private Actor actor;
    private InputButtons appliedButton;
    private bool hasApplied;

    private void Awake()
    {
        actor=GetComponent<Actor>();
    }

    private void Update()
    {
        if(actor==null||actor.inputSystem?.playerController==null||
           !actor.IsOwner)
            return;

        if(hasApplied&&appliedButton!=button)
            actor.inputSystem.playerController.SetForcedHeld(
                appliedButton,
                false);

        actor.inputSystem.playerController.SetForcedHeld(button,holdInput);
        appliedButton=button;
        hasApplied=true;
    }

    private void OnDisable()
    {
        if(!hasApplied||actor==null||actor.inputSystem?.playerController==null)
            return;

        actor.inputSystem.playerController.SetForcedHeld(
            appliedButton,
            false);
        hasApplied=false;
    }
}
