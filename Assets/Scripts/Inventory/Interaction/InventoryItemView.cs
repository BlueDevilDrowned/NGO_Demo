using System.Collections.Generic;
using InventorySolver;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class InventoryItemView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public InventoryRuntime.Entry Entry { get; private set; }
    public ERotation Rotation { get; private set; }
    public System.Action<InventoryItemView, PointerEventData> BeginDrag;
    public System.Action<InventoryItemView, PointerEventData> Dragged;
    public System.Action<InventoryItemView, PointerEventData> EndDrag;
    public System.Action<InventoryItemView, PointerEventData> ContextRequested;

    private readonly List<Image> occupiedCells = new List<Image>();
    private RectTransform occupiedLayer;
    private Image icon;
    private Image raycastImage;
    private InventoryRegionCoordinateMap map;
    private InventoryUIConfigSO config;

    public void Bind(InventoryRuntime.Entry entry)
    {
        Entry = entry;
        Rotation = entry != null ? entry.Placement.Rotation : ERotation.R0;
        EnsureVisualChildren();
    }

    public void RefreshVisual(InventoryRegionCoordinateMap coordinateMap, InventoryUIConfigSO uiConfig)
    {
        map = coordinateMap;
        config = uiConfig;
        EnsureVisualChildren();
        if (Entry == null)
        {
            gameObject.SetActive(false);
            return;
        }

        StorageShapeModule shape = FindShape(Entry.Item);
        IReadOnlyList<Vector2Int> cells = InventoryGeometry.Occupied(shape, ERotation.R0);
        EnsureOccupiedPool(cells.Count);
        RectInt bounds = InventoryGeometry.Bounds(cells);
        float width = bounds.width * map.CellSize + Mathf.Max(0, bounds.width - 1) * map.Spacing;
        float height = bounds.height * map.CellSize + Mathf.Max(0, bounds.height - 1) * map.Spacing;
        RectTransform rect = transform as RectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.Euler(0f, 0f, RotationAngle());
        Vector2 pivotOffset = CanonicalPivotOffset(bounds);
        Vector2Int logicalAnchor = new Vector2Int(
            Entry.Placement.Anchor.X,
            Entry.Placement.Anchor.Y);
        Vector2 logicalAnchorPosition = map.CellCenter(logicalAnchor);
        Vector2Int rotationOffset = InventoryGeometry.RotationAnchorOffset(shape, Rotation);
        Vector2 logicalOffsetFromPivot = new Vector2(
            rotationOffset.x * map.Step,
            -rotationOffset.y * map.Step);
        Vector2 pivotPosition = logicalAnchorPosition - logicalOffsetFromPivot;
        rect.anchoredPosition = pivotPosition - RotateOffset(pivotOffset);
        rect.sizeDelta = new Vector2(width, height);

        icon.sprite = Entry.Item.Definition != null ? Entry.Item.Definition.Icon : null;
        icon.preserveAspect = config == null || config.preserveItemAspect;
        icon.color = new Color(1f, 1f, 1f, config == null ? 1f : config.itemOpacity);
        icon.raycastTarget = false;
        raycastImage.color = new Color(1f, 1f, 1f, 0.001f);

        Color cellColor = FindShape(Entry.Item) != null
            ? FindShape(Entry.Item).QualityColor
            : new Color(1f, 1f, 1f, 0.2f);
        cellColor.a = Mathf.Clamp01(cellColor.a <= 0f ? 0.28f : cellColor.a);
        for (int i = 0; i < occupiedCells.Count; i++)
        {
            bool active = i < cells.Count;
            occupiedCells[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            Vector2Int cell = cells[i];
            RectTransform cellRect = occupiedCells[i].rectTransform;
            cellRect.anchoredPosition = new Vector2(
                (cell.x - bounds.xMin) * map.Step + map.CellSize * 0.5f - width * 0.5f,
                -(cell.y - bounds.yMin) * map.Step - map.CellSize * 0.5f + height * 0.5f);
            cellRect.sizeDelta = new Vector2(map.CellSize, map.CellSize);
            occupiedCells[i].color = cellColor;
        }
    }

    public void RestoreVisualPlacement(InventoryRegionCoordinateMap coordinateMap)
    {
        if (Entry == null)
        {
            return;
        }

        Rotation = Entry.Placement.Rotation;
        RefreshVisual(coordinateMap, config);
    }

    public void Rotate()
    {
        StorageShapeModule shape = FindShape(Entry != null ? Entry.Item : null);
        if (shape != null && !shape.CanRotate)
        {
            return;
        }

        Rotation = (ERotation)(((int)Rotation + 1) % 4);
        RefreshVisual(map, config);
    }

    public Vector3 GetRotationPivotWorldPosition()
    {
        if (Entry == null)
        {
            return transform.position;
        }

        IReadOnlyList<Vector2Int> cells = InventoryGeometry.Occupied(FindShape(Entry.Item), ERotation.R0);
        RectInt bounds = InventoryGeometry.Bounds(cells);
        return (transform as RectTransform).TransformPoint(CanonicalPivotOffset(bounds));
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            ContextRequested?.Invoke(this, eventData);
            return;
        }

        BeginDrag?.Invoke(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        Dragged?.Invoke(this, eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            return;
        }

        EndDrag?.Invoke(this, eventData);
    }

    private void EnsureVisualChildren()
    {
        if (raycastImage == null)
        {
            raycastImage = GetComponent<Image>();
            if (raycastImage == null)
            {
                raycastImage = gameObject.AddComponent<Image>();
            }
            raycastImage.raycastTarget = true;
        }

        if (occupiedLayer == null)
        {
            Transform existing = transform.Find("OccupiedCells");
            GameObject layerObject = existing != null
                ? existing.gameObject
                : new GameObject("OccupiedCells", typeof(RectTransform));
            if (existing == null)
            {
                layerObject.transform.SetParent(transform, false);
            }
            occupiedLayer = layerObject.GetComponent<RectTransform>();
            ConfigureFullRect(occupiedLayer);
        }

        if (icon == null)
        {
            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(transform, false);
            icon = iconObject.GetComponent<Image>();
            ConfigureFullRect(icon.rectTransform);
        }

        icon.transform.SetAsLastSibling();
    }

    private void EnsureOccupiedPool(int count)
    {
        while (occupiedCells.Count < count)
        {
            GameObject cellObject = new GameObject("OccupiedCell", typeof(RectTransform), typeof(Image));
            cellObject.transform.SetParent(occupiedLayer, false);
            Image cell = cellObject.GetComponent<Image>();
            cell.raycastTarget = false;
            cell.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            cell.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            occupiedCells.Add(cell);
        }
    }

    private static void ConfigureFullRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Vector2 CanonicalPivotOffset(RectInt bounds)
    {
        return new Vector2(
            -bounds.xMin * map.Step +
                map.CellSize * 0.5f -
                (bounds.width * map.CellSize +
                    Mathf.Max(0, bounds.width - 1) * map.Spacing) * 0.5f,
            bounds.yMin * map.Step -
                map.CellSize * 0.5f +
                (bounds.height * map.CellSize +
                    Mathf.Max(0, bounds.height - 1) * map.Spacing) * 0.5f);
    }

    private Vector2 RotateOffset(Vector2 offset)
    {
        return Quaternion.Euler(0f, 0f, RotationAngle()) * offset;
    }

    private float RotationAngle()
    {
        StorageShapeModule shape = FindShape(Entry != null ? Entry.Item : null);
        if (shape != null && !shape.CanRotate)
        {
            return 0f;
        }

        return -(int)Rotation * 90f;
    }

    private static StorageShapeModule FindShape(ItemInstance item)
    {
        if (item == null || item.Modules == null)
        {
            return null;
        }

        for (int i = 0; i < item.Modules.Count; i++)
        {
            if (item.Modules[i] is StorageShapeModule shape)
            {
                return shape;
            }
        }

        return null;
    }
}
