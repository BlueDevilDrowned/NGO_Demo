using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "ItemDefinition")]
public class InventoryItemDefinition : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;
    [SerializeReference] public List<ItemModule> Modules = new List<ItemModule> { new StorageShapeModule() };

    protected virtual void OnValidate()
    {
        if (Modules == null) Modules = new List<ItemModule>();
    }
}
