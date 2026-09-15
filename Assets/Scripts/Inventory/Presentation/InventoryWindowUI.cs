using System;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InventoryWindowUI : MonoBehaviour, IWindowStackItem
{
    private static InventoryWindowUI current;
    private static InventoryWindowUI cached;
    private BackpackView page;
    private InventorySystem inventory;
    private InventoryContextMenu contextMenu;

    public static bool IsOpen
    {
        get { return current != null && current.gameObject.activeSelf; }
    }

    public InventorySystem Inventory
    {
        get { return inventory; }
    }

    public static void RotateCurrentItem()
    {
        if (current != null)
        {
            current.page.RotateCurrentItem();
        }
    }

    public static void Toggle(InventorySystem system)
    {
        if (IsOpen)
        {
            WindowStack.Instance.CloseTop();
            return;
        }

        if (system == null || system.ActiveBackpack == null)
        {
            return;
        }

        InventoryWindowUI window = Open(system);
        WindowStack.Instance.Push(window);
    }

    public static InventoryWindowUI Open(InventorySystem system)
    {
        if (cached == null)
        {
            GameObject root = new GameObject("InventoryWindowUI", typeof(RectTransform));
            root.transform.SetParent(WindowStack.Instance.Root, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            cached = root.AddComponent<InventoryWindowUI>();
            cached.page = BackpackView.Create(root.transform);
            cached.contextMenu = InventoryContextMenu.Create(root.transform);
            cached.page.ContextRequested = cached.ShowContextMenu;
        }

        cached.inventory = system;
        current = cached;
        cached.gameObject.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        cached.inventory.InventoryChanged -= cached.Refresh;
        cached.inventory.InventoryChanged += cached.Refresh;
        cached.Refresh();
        return cached;
    }

    public event Action Closed;

    public void Refresh()
    {
        if (inventory != null && inventory.ActiveBackpack != null && page != null)
        {
            page.Build(inventory.ActiveBackpack, inventory.Runtime != null ? inventory.Runtime.Entries : null, inventory);
        }
    }

    private void ShowContextMenu(
        InventoryItemView itemView,
        PointerEventData eventData)
    {
        if (itemView?.Entry == null || inventory == null)
        {
            return;
        }

        int instanceId = itemView.Entry.InstanceId;
        contextMenu.Show(
            itemView.Entry,
            inventory.Owner,
            eventData.position,
            eventData.pressEventCamera,
            option => inventory.RequestBackpackInteraction(instanceId, option.Id));
    }

    public void CloseFromStack()
    {
        Close();
    }

    public void Close()
    {
        contextMenu?.Hide();
        WindowStack.Instance.Remove(this);
        if (inventory != null)
        {
            inventory.InventoryChanged -= Refresh;
        }

        if (ReferenceEquals(current, this))
        {
            current = null;
        }

        Closed?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (ReferenceEquals(current, this))
        {
            current = null;
        }
        if (ReferenceEquals(cached, this))
        {
            cached = null;
        }
    }
}
