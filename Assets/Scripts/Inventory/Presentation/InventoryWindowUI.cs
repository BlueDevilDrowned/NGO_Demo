using UnityEngine;

using System;
public sealed class InventoryWindowUI : MonoBehaviour, IWindowStackItem
{
    private static InventoryWindowUI current;
    public static bool IsOpen => current != null;
    public event Action Closed;
    private BackpackView page;
    private InventorySystem inventory;
    public static InventoryWindowUI Open(InventorySystem inventory)
    {
        var root = new GameObject("InventoryWindowUI", typeof(RectTransform));
        root.transform.SetParent(WindowStack.Instance.Root, false);
        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var ui = root.AddComponent<InventoryWindowUI>();
        current = ui;
        ui.inventory = inventory;
        ui.page = BackpackView.Create(root.transform);
        inventory.InventoryChanged += ui.Refresh;
        ui.Refresh();
        return ui;
    }
    public static void Toggle(InventorySystem inventory) { if (current != null) { WindowStack.Instance.CloseTop(); return; } if (inventory?.ActiveBackpack != null) { var window = Open(inventory); WindowStack.Instance.Push(window); } }
    public void Refresh()
    {
        if (inventory?.ActiveBackpack != null) page.Build(inventory.ActiveBackpack, inventory.Runtime?.Entries, inventory);
    }
    public void CloseFromStack() { Close(); }
    public void Close() { if (inventory != null) inventory.InventoryChanged -= Refresh; if (ReferenceEquals(current, this)) current = null; Closed?.Invoke(); Closed = null; if (this != null) Destroy(gameObject); }
}
