using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class BackpackView : MonoBehaviour
{
    public System.Action<InventoryItemView, PointerEventData> ContextRequested;
    [SerializeField] private InventoryUIConfigSO uiConfig;
    [SerializeField] private RectTransform designSpace;
    [SerializeField] private BackpackGridView regionPrefab;
    [SerializeField] private float spacingPixels = 1f;

    private readonly List<BackpackGridView> regions = new List<BackpackGridView>();
    private InventoryDragController dragController;
    private InventoryGridMetrics metrics;

    public void RotateCurrentItem()
    {
        if (dragController != null)
        {
            dragController.RotateCurrent();
        }
    }

    public void SetVisible(bool value)
    {
        if (!value && dragController != null)
        {
            dragController.CancelDrag();
        }

        gameObject.SetActive(value);
    }

    public static BackpackView Create(Transform parent)
    {
        BackpackView prefab = Resources.Load<BackpackView>("UI/BackpackPage");
        if (prefab != null)
        {
            return Instantiate(prefab, parent);
        }

        return CreateDefault(parent);
    }

    public void Build(
        BackpackModule module,
        IReadOnlyList<InventoryRuntime.Entry> entries,
        InventorySystem inventory)
    {
        if (module == null || module.Regions == null || module.Regions.Count == 0)
        {
            return;
        }

        EnsureReferences();
        Canvas.ForceUpdateCanvases();
        float spacing = uiConfig != null ? uiConfig.cellSpacing : spacingPixels;
        float cellSize = CalculateSharedCellSize(module, spacing);
        metrics = new InventoryGridMetrics(cellSize, spacing);

        while (regions.Count < module.Regions.Count)
        {
            BackpackGridView region = Instantiate(regionPrefab, designSpace);
            regions.Add(region);
        }

        for (int i = 0; i < regions.Count; i++)
        {
            bool active = i < module.Regions.Count && module.Regions[i] != null;
            regions[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            InventoryRegionDefinition definition = module.Regions[i];
            ConfigureRegionTransform(regions[i].transform as RectTransform, definition, metrics);
            regions[i].Initialize(dragController, uiConfig);
            regions[i].Build(definition, metrics);
            regions[i].ConfigureInteraction(inventory, definition, i);
            regions[i].ContextRequested = ContextRequested;
        }

        dragController.ConfigureRegions(regions);
        for (int i = 0; i < regions.Count; i++)
        {
            if (!regions[i].gameObject.activeSelf)
            {
                continue;
            }

            List<InventoryRuntime.Entry> regionEntries = new List<InventoryRuntime.Entry>();
            if (entries != null)
            {
                for (int j = 0; j < entries.Count; j++)
                {
                    InventoryRuntime.Entry entry = entries[j];
                    if (entry != null && entry.Placement.RegionIndex == i)
                    {
                        regionEntries.Add(entry);
                    }
                }
            }

            regions[i].DrawItems(regionEntries);
        }
    }

    private void EnsureReferences()
    {
        if (uiConfig == null)
        {
            uiConfig = Resources.Load<InventoryUIConfigSO>("UI/InventoryUIConfig");
        }
        if (designSpace == null)
        {
            designSpace = transform as RectTransform;
        }

        ConfigureDesignSpace(designSpace);

        if (regionPrefab == null)
        {
            regionPrefab = CreateRegionPrefab(transform);
        }

        if (dragController == null)
        {
            dragController = GetComponent<InventoryDragController>();
            if (dragController == null)
            {
                dragController = gameObject.AddComponent<InventoryDragController>();
            }
        }
    }

    private float CalculateSharedCellSize(BackpackModule module, float spacing)
    {
        Rect parent = designSpace.rect;
        float result = float.MaxValue;
        for (int i = 0; i < module.Regions.Count; i++)
        {
            InventoryRegionDefinition definition = module.Regions[i];
            if (definition == null)
            {
                continue;
            }

            Rect normalized = definition.NormalizedRect;
            float width = Mathf.Abs(parent.width * normalized.width);
            float height = Mathf.Abs(parent.height * normalized.height);
            float candidateWidth = (width - spacing * Mathf.Max(0, definition.Width - 1)) / definition.Width;
            float candidateHeight = (height - spacing * Mathf.Max(0, definition.Height - 1)) / definition.Height;
            result = Mathf.Min(result, candidateWidth, candidateHeight);
        }

        if (result == float.MaxValue || result <= 0f)
        {
            result = Mathf.Max(1f, Mathf.Min(parent.width, parent.height));
        }

        return result;
    }

    private static void ConfigureRegionTransform(
        RectTransform rect,
        InventoryRegionDefinition definition,
        InventoryGridMetrics metrics)
    {
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        Rect normalized = definition.NormalizedRect;
        // NormalizedRect stores its y coordinate from the bottom, while this
        // RectTransform uses a top-left pivot. Its top edge is therefore yMax.
        rect.anchorMin = new Vector2(normalized.xMin, normalized.yMax);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(
            definition.Width * metrics.CellSize + Mathf.Max(0, definition.Width - 1) * metrics.Spacing,
            definition.Height * metrics.CellSize + Mathf.Max(0, definition.Height - 1) * metrics.Spacing);
    }

    private static void ConfigureDesignSpace(RectTransform rect)
    {
        if (rect == null)
        {
            return;
        }

        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static BackpackView CreateDefault(Transform parent)
    {
        GameObject root = new GameObject("BackpackPage", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        RectTransform pageRect = root.GetComponent<RectTransform>();
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = Vector2.zero;
        pageRect.offsetMax = Vector2.zero;
        BackpackView view = root.AddComponent<BackpackView>();
        view.designSpace = pageRect;
        view.regionPrefab = CreateRegionPrefab(root.transform);
        return view;
    }

    private static BackpackGridView CreateRegionPrefab(Transform parent)
    {
        GameObject root = new GameObject("Region", typeof(RectTransform), typeof(BackpackGridView));
        root.transform.SetParent(parent, false);
        root.SetActive(false);
        return root.GetComponent<BackpackGridView>();
    }
}
