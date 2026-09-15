using System;
using System.Collections.Generic;

[Serializable]
public abstract class BackpackInteractionModule
{
    public virtual bool CanShow(InventoryRuntime.Entry entry, Actor actor)
    {
        return true;
    }

    public virtual bool CanInteract(InventoryRuntime.Entry entry, Actor actor)
    {
        return false;
    }

    public abstract void CollectInteractionOptions(
        InventoryRuntime.Entry entry,
        List<BackpackInteractionOption> options);

    public abstract bool OnInteract(
        InventoryRuntime.Entry entry,
        Actor actor,
        string optionId);
}
