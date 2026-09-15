using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class InventoryContextMenu : MonoBehaviour,
    IPointerClickHandler
{
    private const float MenuWidth = 180f;
    private const float RowHeight = 36f;
    private const float ScreenPadding = 8f;
    private const float MouseOffset = 6f;

    private RectTransform panel;
    private readonly List<GameObject> rows = new List<GameObject>();

    public static InventoryContextMenu Create(Transform parent)
    {
        GameObject root = new GameObject(
            "InventoryContextMenu",
            typeof(RectTransform),
            typeof(Image),
            typeof(InventoryContextMenu));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image blocker = root.GetComponent<Image>();
        blocker.color = Color.clear;
        blocker.raycastTarget = true;

        InventoryContextMenu menu = root.GetComponent<InventoryContextMenu>();
        menu.EnsurePanel();
        root.SetActive(false);
        return menu;
    }

    public void Show(
        InventoryRuntime.Entry entry,
        Actor actor,
        Vector2 screenPosition,
        Camera eventCamera,
        System.Action<BackpackInteractionOption> execute)
    {
        ClearRows();
        if (entry?.Item == null || actor == null)
        {
            Hide();
            return;
        }

        var options = new List<BackpackInteractionOption>();
        entry.Item.CollectBackpackInteractionOptions(entry, actor, options);
        if (options.Count == 0)
        {
            Hide();
            return;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        panel.sizeDelta = new Vector2(MenuWidth, options.Count * RowHeight);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            screenPosition,
            eventCamera,
            out Vector2 localPosition);
        Vector2 desiredPosition = localPosition +
            new Vector2(MouseOffset, -MouseOffset);
        panel.anchoredPosition = ClampToRoot(desiredPosition);

        for (int i = 0; i < options.Count; i++)
        {
            BackpackInteractionOption option = options[i];
            CreateRow(option, i, execute);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Hide();
    }

    private void EnsurePanel()
    {
        GameObject panelObject = new GameObject(
            "Panel",
            typeof(RectTransform),
            typeof(Image));
        panelObject.transform.SetParent(transform, false);
        panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0f, 1f);
        panelObject.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.11f, 0.98f);
    }

    private void CreateRow(
        BackpackInteractionOption option,
        int index,
        System.Action<BackpackInteractionOption> execute)
    {
        GameObject row = new GameObject(
            "Action_" + option.Id,
            typeof(RectTransform),
            typeof(Image),
            typeof(Button));
        row.transform.SetParent(panel, false);
        rows.Add(row);

        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * RowHeight);
        rect.sizeDelta = new Vector2(0f, RowHeight);

        Image background = row.GetComponent<Image>();
        background.color = index % 2 == 0
            ? new Color(0.16f, 0.17f, 0.2f, 1f)
            : new Color(0.13f, 0.14f, 0.17f, 1f);

        Button button = row.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.25f, 0.42f, 0.62f, 1f);
        colors.pressedColor = new Color(0.18f, 0.32f, 0.5f, 1f);
        colors.disabledColor = new Color(0.12f, 0.13f, 0.15f, 0.8f);
        button.colors = colors;
        button.interactable = option.Available;
        button.onClick.AddListener(() =>
        {
            execute?.Invoke(option);
            Hide();
        });

        GameObject labelObject = new GameObject(
            "Label",
            typeof(RectTransform),
            typeof(Text));
        labelObject.transform.SetParent(row.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(-12f, 0f);

        if (option.Icon != null)
        {
            GameObject iconObject = new GameObject(
                "Icon",
                typeof(RectTransform),
                typeof(Image));
            iconObject.transform.SetParent(row.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(10f, 0f);
            iconRect.sizeDelta = new Vector2(22f, 22f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = option.Icon;
            icon.preserveAspect = true;
            labelRect.offsetMin = new Vector2(40f, 0f);
        }

        Text label = labelObject.GetComponent<Text>();
        label.text = option.DisplayName;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 15;
        label.alignment = TextAnchor.MiddleLeft;
        label.color = option.Available ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        label.raycastTarget = false;
    }

    private Vector2 ClampToRoot(Vector2 position)
    {
        Rect rootRect = (transform as RectTransform).rect;
        float height = panel.sizeDelta.y;
        float x = Mathf.Clamp(
            position.x,
            rootRect.xMin + ScreenPadding,
            rootRect.xMax - MenuWidth - ScreenPadding);
        float y = Mathf.Clamp(
            position.y,
            rootRect.yMin + height + ScreenPadding,
            rootRect.yMax - ScreenPadding);
        return new Vector2(x, y);
    }

    private void ClearRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            Destroy(rows[i]);
        }

        rows.Clear();
    }
}
