using System.Collections.Generic;
using UnityEngine;

public sealed class BackpackView : MonoBehaviour
{
    [SerializeField] private InventoryUIConfigSO uiConfig;
    [SerializeField] private RectTransform designSpace;
    [SerializeField] private BackpackGridView regionPrefab;
    [SerializeField] private float spacingPixels = 0f;
    private readonly List<BackpackGridView> regions = new();
    public void SetVisible(bool value) => gameObject.SetActive(value);

    public static BackpackView Create(Transform parent = null)
    {
        BackpackView prefab = Resources.Load<BackpackView>("UI/BackpackPage");
        BackpackView view = prefab != null
            ? Instantiate(prefab, parent)
            : CreateDefault(parent);
        return view;
    }

    private static BackpackView CreateDefault(Transform parent)
    {
        var root = new GameObject("BackpackPage", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var pageRect = root.GetComponent<RectTransform>();
        pageRect.anchorMin = Vector2.zero;
        pageRect.anchorMax = Vector2.one;
        pageRect.offsetMin = pageRect.offsetMax = Vector2.zero;
        var view = root.AddComponent<BackpackView>();
        var design = new GameObject("DesignSpace", typeof(RectTransform));
        design.transform.SetParent(root.transform, false);
        var rect = design.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        view.designSpace = rect;
        view.regionPrefab = CreateRegionPrefab(design.transform);
        return view;
    }

    private static BackpackGridView CreateRegionPrefab(Transform parent)
    {
        var root = new GameObject("Region", typeof(RectTransform), typeof(BackpackGridView));
        root.transform.SetParent(parent, false);
        root.SetActive(false);
        return root.GetComponent<BackpackGridView>();
    }

    public void Build(BackpackModule module, IReadOnlyList<InventoryRuntime.Entry> entries = null, InventorySystem inventory = null)
    {
        Canvas.ForceUpdateCanvases();
        if (module?.Regions == null || designSpace == null || regionPrefab == null) return;
        while (regions.Count < module.Regions.Count)
            regions.Add(Instantiate(regionPrefab, designSpace));
        for (int i = 0; i < regions.Count; i++)
        {
            bool active = i < module.Regions.Count && module.Regions[i] != null;
            regions[i].gameObject.SetActive(active);
            if (!active) continue;
            InventoryRegionDefinition region = module.Regions[i];
            Rect normalized = region.NormalizedRect;
            RectTransform rect = regions[i].GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(normalized.xMin, normalized.yMin);
            rect.anchorMax = new Vector2(normalized.xMax, normalized.yMax);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var regionEntries = new List<InventoryRuntime.Entry>();
            if (entries != null) foreach (var entry in entries)
                if (entry?.Placement.RegionIndex == i) regionEntries.Add(entry);
            float cell = Mathf.Min(rect.rect.width / region.Width, rect.rect.height / region.Height);
            regions[i].Build(region, cell);
            regions[i].ConfigureInteraction(inventory, region, i);
            regions[i].DrawItems(regionEntries, cell, uiConfig != null ? uiConfig.cellSpacing : spacingPixels);
        }
    }
}
