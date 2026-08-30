using UnityEngine;

[DisallowMultipleComponent]
public sealed class AimTargetPresentation : MonoBehaviour
{
    [SerializeField]private Actor actor;
    [SerializeField]private Transform targetOutput;

    public Transform TargetOutput=>targetOutput;

    private void Awake()
    {
        actor??=GetComponentInParent<Actor>();
    }

    private void LateUpdate()
    {
        if(actor==null||targetOutput==null||
           !actor.IsSpawned||actor.aimSystem==null)
            return;

        AimData aim=actor.IsOwner
            ?actor.aimSystem.data
            :actor.simulation.aimData;
        targetOutput.position=aim.TargetPosition;
    }
}
