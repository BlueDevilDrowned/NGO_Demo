using System.Collections.Generic;
using UnityEngine;

public sealed class BackpackView : MonoBehaviour
{
    [SerializeField] private RectTransform designSpace;
    [SerializeField] private BackpackGridView regionPrefab;
    [SerializeField] private float spacingPixels = 4f;
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
        return root.GetComponent<BackpackGridView>();
    }

    public void Build(BackpackModule module)
    {
        if (module?.Regions == null || designSpace == null || regionPrefab == null) return;
        while (regions.Count < module.Regions.Count)
            regions.Add(Instantiate(regionPrefab, designSpace));
        Vector2 size = designSpace.rect.size;
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
            float cell = Mathf.Min(rect.rect.width / region.Width, rect.rect.height / region.Height);
            regions[i].Build(region, cell, spacingPixels);
        }
    }
}
