using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BackpackGridView : MonoBehaviour
{
    public System.Action<InventoryItemView, UnityEngine.EventSystems.PointerEventData> ContextRequested;
    public InventoryRegionDefinition Region { get; private set; }
    public int RegionIndex { get; private set; }
    public InventoryRegionCoordinateMap CoordinateMap { get; private set; }
    public InventoryInteractionOverlay WarningOverlay { get; private set; }
    public RectTransform ItemLayer { get; private set; }
    public Color AllowedPreviewColor
    {
        get { return uiConfig != null ? uiConfig.allowedCellColor : new Color(0.2f, 1f, 0.2f, 0.35f); }
    }
    public Color WarningPreviewColor
    {
        get { return uiConfig != null ? uiConfig.warningCellColor : new Color(1f, 0.1f, 0.1f, 0.5f); }
    }
    public Color OriginalShadowColor
    {
        get { return uiConfig != null ? uiConfig.originalPlacementColor : new Color(1f, 1f, 1f, 0.18f); }
    }

    private readonly List<Image> normalCells = new List<Image>();
    private readonly List<Image> allowedCells = new List<Image>();
    private readonly Dictionary<int, InventoryItemView> itemViews = new Dictionary<int, InventoryItemView>();
    private RectTransform normalLayer;
    private RectTransform allowedLayer;
    private RectTransform warningLayer;
    private InventoryDragController dragController;
    private InventoryUIConfigSO uiConfig;

    public void Initialize(InventoryDragController controller, InventoryUIConfigSO config)
    {
        dragController = controller;
        uiConfig = config;
        EnsureLayers();
    }

    public void ConfigureInteraction(InventorySystem system, InventoryRegionDefinition definition, int index)
    {
        Region = definition;
        RegionIndex = index;
    }

    public void Build(InventoryRegionDefinition definition, InventoryGridMetrics metrics)
    {
        Region = definition;
        CoordinateMap = new InventoryRegionCoordinateMap(metrics.CellSize, metrics.Spacing);
        EnsureLayers();
        ConfigureLayerRect(normalLayer);
        ConfigureLayerRect(allowedLayer);
        ConfigureLayerRect(warningLayer);
        ConfigureLayerRect(ItemLayer);

        int count = definition.Width * definition.Height;
        EnsureCellPool(normalCells, normalLayer, count, "NormalCell");
        EnsureCellPool(allowedCells, allowedLayer, count, "AllowedCell");
        HashSet<Vector2Int> enabled = new HashSet<Vector2Int>(definition.EnabledCells ?? new List<Vector2Int>());
        for (int i = 0; i < normalCells.Count; i++)
        {
            bool active = i < count;
            normalCells[i].gameObject.SetActive(active);
            allowedCells[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            Vector2Int cell = new Vector2Int(i % definition.Width, i / definition.Width);
            ConfigureCell(normalCells[i], cell, metrics, enabled.Contains(cell));
            ConfigureCell(allowedCells[i], cell, metrics, enabled.Contains(cell));
            allowedCells[i].color = uiConfig != null
                ? uiConfig.allowedCellColor
                : new Color(0.2f, 1f, 0.2f, 0.08f);
            allowedCells[i].gameObject.SetActive(enabled.Contains(cell));
        }
        if (WarningOverlay != null)
        {
            WarningOverlay.Configure(CoordinateMap);
        }
    }

    public void DrawItems(IEnumerable<InventoryRuntime.Entry> entries)
    {
        HashSet<int> active = new HashSet<int>();
        if (entries != null)
        {
            foreach (InventoryRuntime.Entry entry in entries)
            {
                if (entry == null || entry.Placement.RegionIndex != RegionIndex)
                {
                    continue;
                }

                active.Add(entry.InstanceId);
                InventoryItemView itemView = GetOrCreateItem(entry.InstanceId);
                itemView.transform.SetParent(ItemLayer, false);
                itemView.Bind(entry);
                itemView.RefreshVisual(CoordinateMap, uiConfig);
                itemView.gameObject.SetActive(true);
                if (dragController != null)
                {
                    dragController.Attach(itemView);
                }
                itemView.ContextRequested = (view, eventData) =>
                    ContextRequested?.Invoke(view, eventData);
            }
        }

        foreach (KeyValuePair<int, InventoryItemView> pair in itemViews)
        {
            if (!active.Contains(pair.Key) && pair.Value != null)
            {
                pair.Value.gameObject.SetActive(false);
            }
        }
    }

    public bool RotateCurrentItem()
    {
        return dragController != null && dragController.RotateCurrent();
    }

    private void EnsureLayers()
    {
        normalLayer = EnsureLayer("NormalCells");
        allowedLayer = EnsureLayer("AllowedCells");
        warningLayer = EnsureLayer("WarningCells");
        ItemLayer = EnsureLayer("Items");
        if (WarningOverlay == null)
        {
            WarningOverlay = warningLayer.gameObject.GetComponent<InventoryInteractionOverlay>();
            if (WarningOverlay == null)
            {
                WarningOverlay = warningLayer.gameObject.AddComponent<InventoryInteractionOverlay>();
            }
        }
    }

    private RectTransform EnsureLayer(string name)
    {
        Transform existing = transform.Find(name);
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject layerObject = new GameObject(name, typeof(RectTransform));
        layerObject.transform.SetParent(transform, false);
        RectTransform rect = layerObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void ConfigureLayerRect(RectTransform rect)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = ((RectTransform)transform).rect.size;
    }

    private static void EnsureCellPool(List<Image> pool, RectTransform parent, int count, string name)
    {
        while (pool.Count < count)
        {
            GameObject cellObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            cellObject.transform.SetParent(parent, false);
            Image image = cellObject.GetComponent<Image>();
            image.raycastTarget = false;
            image.rectTransform.anchorMin = new Vector2(0f, 1f);
            image.rectTransform.anchorMax = new Vector2(0f, 1f);
            image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            pool.Add(image);
        }
    }

    private void ConfigureCell(Image image, Vector2Int cell, InventoryGridMetrics metrics, bool enabled)
    {
        image.rectTransform.anchoredPosition = CoordinateMap.CellCenter(cell);
        image.rectTransform.sizeDelta = new Vector2(metrics.CellSize, metrics.CellSize);
        image.color = enabled
            ? (uiConfig != null ? uiConfig.normalCellColor : new Color(1f, 1f, 1f, 0.12f))
            : (uiConfig != null ? uiConfig.disabledCellColor : new Color(1f, 1f, 1f, 0.025f));
    }

    private InventoryItemView GetOrCreateItem(int instanceId)
    {
        if (itemViews.TryGetValue(instanceId, out InventoryItemView existing) && existing != null)
        {
            return existing;
        }

        GameObject itemObject = new GameObject("Item_" + instanceId, typeof(RectTransform), typeof(InventoryItemView));
        itemObject.transform.SetParent(ItemLayer, false);
        InventoryItemView created = itemObject.GetComponent<InventoryItemView>();
        itemViews[instanceId] = created;
        return created;
    }
}
