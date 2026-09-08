using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Registry", fileName = "InventoryItemRegistry")]
public sealed class InventoryItemRegistry : ScriptableObject
{
    [Serializable]
    public sealed class Entry { public string Id; public InventoryItemDefinition Item; }
    public List<Entry> Items = new List<Entry>();

    public InventoryItemDefinition Find(string id)
    {
        foreach (Entry entry in Items)
            if (entry != null && entry.Id == id) return entry.Item;
        return null;
    }

    private void OnValidate()
    {
        var used = new HashSet<string>();
        foreach (Entry entry in Items)
        {
            if (entry == null) continue;
            entry.Id = (entry.Id ?? string.Empty).Trim();
            if (entry.Id.Length == 0 || !used.Add(entry.Id))
                Debug.LogWarning("物品表中存在空 ID 或重复 ID。", this);
        }
    }
}
