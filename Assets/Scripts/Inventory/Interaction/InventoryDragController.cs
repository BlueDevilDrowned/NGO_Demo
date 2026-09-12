using UnityEngine;
using UnityEngine.EventSystems;
using InventorySolver;

public sealed class InventoryDragController : MonoBehaviour
{
    private InventoryItemView dragging;
    private InventorySolver.Placement originalPlacement;
    private bool draggingActive;
    private InventorySystem inventory;
    private InventoryRegionDefinition region;
    private int regionIndex;
    private InventoryRegionCoordinateMap coordinateMap;
    private Placement candidatePlacement;
    private bool candidateValid;
    private Vector2 dragStartMouse;
    private Vector3 dragStartPosition;
    private InventoryInteractionOverlay overlay;
    private Vector2 pointerOffset;
    public void Attach(InventoryItemView item, InventoryInteractionOverlay preview)
    {
        dragging = item; overlay = preview;
        item.BeginDrag = Begin;
        item.Dragged = Move;
        item.EndDrag = End;
    }
    public void Configure(InventorySystem system, InventoryRegionDefinition targetRegion, int targetRegionIndex)
    {
        inventory = system;
        region = targetRegion;
        regionIndex = targetRegionIndex;
    }

    public void ConfigureMap(InventoryRegionCoordinateMap map)
    {
        coordinateMap = map;
    }
    private void Begin(InventoryItemView item, PointerEventData data)
    {
        dragging = item;
        originalPlacement = item.Entry.Placement;
        dragStartMouse = data.position;
        dragStartPosition = item.transform.position;
        draggingActive = true;
        candidatePlacement = originalPlacement;
    }
    private void Move(InventoryItemView item, PointerEventData data)
    {
        if (item == null) return;
        item.transform.position = dragStartPosition + (Vector3)(data.position - dragStartMouse);
        if (region == null || inventory?.Runtime == null || overlay == null)
            return;

        RectTransform regionRect = transform as RectTransform;
        Camera eventCamera = data.pressEventCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                regionRect,
                data.position,
                eventCamera,
                out Vector2 localPosition))
        {
            return;
        }

        Vector2Int anchor = coordinateMap.ScreenToCell(localPosition);
        StorageShapeModule shape = FindShape(item.Entry.Item);
        if (shape == null)
            return;

        var placement = new Placement(
            regionIndex,
            new Cell(anchor.x, anchor.y),
            item.Rotation);
        var preview = new InventoryPlacementPreview();
        preview.Evaluate(shape, placement, region, inventory.Runtime.Entries);
        candidatePlacement = placement;
        candidateValid = preview.IsValid;
        overlay.Show(
            preview.Cells,
            coordinateMap,
            preview.IsValid
                ? new Color(0.2f, 1f, 0.2f, .35f)
                : new Color(1f, .1f, .1f, .5f));
    }
    private void End(InventoryItemView item, PointerEventData data)
    {
        if (draggingActive && dragging != null && inventory != null)
        {
            if (candidateValid)
            {
                inventory.RequestPlacement(
                    dragging.Entry.InstanceId,
                    candidatePlacement.RegionIndex,
                    candidatePlacement.Anchor.X,
                    candidatePlacement.Anchor.Y,
                    candidatePlacement.Rotation);
            }
        }
        if (overlay != null) overlay.Clear();
        draggingActive = false;
        dragging = null;
    }
    public void RotateCurrent()
    {
        if (dragging != null)
            dragging.Rotate();
    }
    private static StorageShapeModule FindShape(ItemInstance item)
    {
        if (item?.Modules == null)
            return null;
        foreach (var module in item.Modules)
        {
            if (module is StorageShapeModule shape)
                return shape;
        }
        return null;
    }
}
