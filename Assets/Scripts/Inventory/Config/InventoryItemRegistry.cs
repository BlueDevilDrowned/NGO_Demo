using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Registry", fileName = "InventoryItemRegistry")]
public sealed class InventoryItemRegistry : ScriptableObject
{
    // The list is the single source of truth. The ID is the one-based index.
    [SerializeField] private List<InventoryItemDefinition> items = new();

    private Dictionary<string, InventoryItemDefinition> itemById;
    private Dictionary<InventoryItemDefinition, string> idByItem;
    private Dictionary<Type, Dictionary<UnityEngine.Object, InventoryItemDefinition>> itemByInfoKey;

    public IReadOnlyList<InventoryItemDefinition> Items => items;

    public InventoryItemDefinition Find(string id)
    {
        EnsureIndexes();
        return !string.IsNullOrEmpty(id) && itemById.TryGetValue(id, out var item) ? item : null;
    }

    public bool TryGetId(InventoryItemDefinition item, out string id)
    {
        EnsureIndexes();
        id = string.Empty;
        return item != null && idByItem.TryGetValue(item, out id);
    }

    public string GetId(InventoryItemDefinition item) =>
        TryGetId(item, out string id) ? id : string.Empty;

    public bool TryFindByInfo<TInfo>(UnityEngine.Object lookupKey, out InventoryItemDefinition item)
        where TInfo : ItemModuleInfo
    {
        EnsureIndexes();
        item = null;
        return lookupKey != null &&
               itemByInfoKey.TryGetValue(typeof(TInfo), out var byKey) &&
               byKey.TryGetValue(lookupKey, out item);
    }

    public string Register(InventoryItemDefinition item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        EnsureIndexes();
        if (idByItem.TryGetValue(item, out string existingId)) return existingId;
        items.Add(item);
        RebuildIndexes();
        return GetId(item);
    }

    public new ItemInstance CreateInstance(string id)
    {
        InventoryItemDefinition item = Find(id);
        return item == null ? null : new ItemInstance(id, item);
    }

    public void RebuildIndexes()
    {
        itemById = new Dictionary<string, InventoryItemDefinition>();
        idByItem = new Dictionary<InventoryItemDefinition, string>();
        itemByInfoKey = new Dictionary<Type, Dictionary<UnityEngine.Object, InventoryItemDefinition>>();

        for (int index = 0; index < items.Count; index++)
        {
            InventoryItemDefinition item = items[index];
            if (item == null) continue;

            string id = (index + 1).ToString();
            if (!itemById.TryAdd(id, item))
                Debug.LogError($"Duplicate item ID in {name}: {id}", this);
            if (!idByItem.TryAdd(item, id))
                Debug.LogError($"Item is registered more than once in {name}: {item.name}", this);

            item.RebuildInfoIndex();
            foreach (ItemModuleInfo info in item.ModuleInfos)
            {
                if (info?.LookupKey == null) continue;
                Type type = info.GetType();
                if (!itemByInfoKey.TryGetValue(type, out var byKey))
                {
                    byKey = new Dictionary<UnityEngine.Object, InventoryItemDefinition>();
                    itemByInfoKey.Add(type, byKey);
                }
                if (!byKey.TryAdd(info.LookupKey, item))
                    Debug.LogError($"Duplicate {type.Name} lookup key in {name}: {info.LookupKey.name}", this);
            }
        }
    }

    private void EnsureIndexes()
    {
        if (itemById == null) RebuildIndexes();
    }

    private void OnValidate()
    {
        if (items == null) items = new List<InventoryItemDefinition>();
        RebuildIndexes();
    }

    private void OnEnable() => RebuildIndexes();
}

