using System;
using UnityEngine;

[Serializable]
public abstract class ItemModuleInfo
{
    [NonSerialized] private InventoryItemDefinition owner;

    public InventoryItemDefinition Owner => owner;
    public virtual Type TargetModuleType => null;
    public virtual UnityEngine.Object LookupKey => null;

    internal void Bind(InventoryItemDefinition value)
    {
        owner = value;
    }
}
