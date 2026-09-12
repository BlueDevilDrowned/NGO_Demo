using UnityEngine;

public sealed class InventoryPresentation : MonoBehaviour
{
    private InventorySystem inventory;
    private InventoryWindowUI window;
    public void Initialize(InventorySystem value) => inventory = value;
    public void Toggle()
    {
        if (window != null) { WindowStack.Instance.CloseTop(); return; }
        if (inventory?.ActiveBackpack == null) return;
        window = InventoryWindowUI.Open(inventory);
        window.Closed += () => window = null;
        WindowStack.Instance.Push(window);
    }
    public void Close() { if (window != null && WindowStack.Instance.IsTop(window)) WindowStack.Instance.CloseTop(); }
}
