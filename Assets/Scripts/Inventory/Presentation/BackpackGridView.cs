using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BackpackGridView : MonoBehaviour
{
    public InventoryRegionCoordinateMap CoordinateMap { get; private set; }
    private InventoryDragController dragController;
    public void ConfigureInteraction(InventorySystem inventory, InventoryRegionDefinition region, int regionIndex)
    {
        dragController ??= GetComponent<InventoryDragController>() ?? gameObject.AddComponent<InventoryDragController>();
        dragController.Configure(inventory, region, regionIndex);
        dragController.ConfigureMap(CoordinateMap);
    }
    private readonly List<Image> cells = new();
    private readonly Dictionary<int, GameObject> itemObjects = new();
    private RectTransform layer;
    public void Build(InventoryRegionDefinition definition, float spacing, float cellSize)
    {
        Build(definition, cellSize);
    }

    public void DrawItems(IEnumerable<InventoryRuntime.Entry> entries, float cellSize, float spacing)
    {
        var active = new HashSet<int>();
        if (entries != null)
        {
            foreach (var entry in entries)
            {
                active.Add(entry.InstanceId);
                if (!itemObjects.TryGetValue(entry.InstanceId, out var itemObject))
                {
                    itemObject = new GameObject("Item_" + entry.InstanceId, typeof(RectTransform), typeof(Image), typeof(InventoryItemView));
                    itemObject.transform.SetParent(transform, false);
                    itemObjects.Add(entry.InstanceId, itemObject);
                }
                var image = itemObject.GetComponent<Image>();
                var itemView = itemObject.GetComponent<InventoryItemView>();
                itemView.Bind(entry);
                dragController.Attach(itemView, null);
                image.sprite = entry.Item?.Definition?.Icon;
                image.preserveAspect = true;
                var shape = FindShape(entry.Item);
                int width = shape != null ? shape.GridWidth : 1;
                int height = shape != null ? shape.GridHeight : 1;
                var rect = image.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.anchoredPosition = CoordinateMap.CellToLocal(new Vector2Int(entry.Placement.Anchor.X, entry.Placement.Anchor.Y));
                rect.sizeDelta = new Vector2(width * cellSize, height * cellSize);
                itemObject.SetActive(true);
            }
        }
        foreach (var pair in itemObjects)
            if (!active.Contains(pair.Key)) pair.Value.SetActive(false);
    }

    private static StorageShapeModule FindShape(ItemInstance item)
    {
        if (item?.Modules == null) return null;
        foreach (var module in item.Modules)
            if (module is StorageShapeModule shape) return shape;
        return null;
    }

    public void Build(InventoryRegionDefinition definition, float cellSize)
    {
        if (definition == null) return;
        layer = transform as RectTransform;
        CoordinateMap = new InventoryRegionCoordinateMap(
            new Vector2(-layer.rect.width * .5f, layer.rect.height * .5f), cellSize, 0f);
        int count = definition.Width * definition.Height;
        while (cells.Count < count)
        {
            var image = new GameObject("Cell", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(transform, false);
            image.raycastTarget = false;
            cells.Add(image);
        }
        var enabled = new HashSet<Vector2Int>(definition.EnabledCells ?? new List<Vector2Int>());
        for (int i = 0; i < cells.Count; i++)
        {
            cells[i].gameObject.SetActive(i < count);
            if (i >= count) continue;
            int x = i % definition.Width;
            int y = i / definition.Width;
            var rect = cells[i].rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.anchoredPosition = CoordinateMap.CellToLocal(new Vector2Int(x, y));
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            cells[i].color = enabled.Contains(new Vector2Int(x, y))
                ? new Color(1, 1, 1, .12f)
                : new Color(1, 1, 1, .025f);
        }
    }
}
