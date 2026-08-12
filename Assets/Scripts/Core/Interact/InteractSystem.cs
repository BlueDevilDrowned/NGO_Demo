using UnityEngine;

public class InteractSystem
{
    private readonly Actor actor;
    private readonly InteractSO config;

    public float RayShowDistance=>config!=null
        ?config.RayShowDistance
        :0f;
    public float RayInteractDistance=>config!=null
        ?config.RayInteractDistance
        :0f;

    public InteractSystem(Actor actor,InteractSO config)
    {
        this.actor=actor;
        this.config=config;
    }
}
