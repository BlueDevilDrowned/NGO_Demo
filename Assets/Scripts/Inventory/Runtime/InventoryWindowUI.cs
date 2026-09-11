using UnityEngine;

public sealed class InventoryWindowUI : MonoBehaviour
{
    private BackpackView page;
    private InventorySystem inventory;
    public static InventoryWindowUI Open(InventorySystem inventory)
    {
        var root = new GameObject("InventoryWindowUI", typeof(RectTransform));
        var ui = root.AddComponent<InventoryWindowUI>();
        ui.inventory = inventory;
        ui.page = BackpackView.Create(root.transform);
        ui.Refresh();
        return ui;
    }
    public void Refresh()
    {
        if (inventory?.ActiveBackpack != null) page.Build(inventory.ActiveBackpack);
    }
    public void Close() { if (this != null) Destroy(gameObject); }
}
