using System.Collections.Generic;
using InventorySolver;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InventoryDragController : MonoBehaviour
{
    private InventoryItemView draggingItem;
    private InventorySystem inventory;
    private IReadOnlyList<BackpackGridView> regions;
    private BackpackGridView sourceRegion;
    private BackpackGridView targetRegion;
    private InventoryInteractionOverlay targetOverlay;
    private Placement originalPlacement;
    private Placement candidatePlacement;
    private Vector2 dragStartMouse;
    private Vector3 dragStartWorldPosition;
    private Vector2 originalPivotScreen;
    private bool dragging;
    private bool candidateValid;
    private PointerEventData lastEventData;

    public void ConfigureRegions(IReadOnlyList<BackpackGridView> views)
    {
        regions = views;
    }

    public void CancelDrag()
    {
        if (!dragging)
        {
            ClearOverlays();
            return;
        }

        if (inventory != null && inventory.Runtime != null && draggingItem != null &&
            draggingItem.Entry != null)
        {
            inventory.Runtime.TrySetLocalPlacement(
                draggingItem.Entry.InstanceId,
                originalPlacement);
            RestoreOriginalVisual();
        }

        ClearOverlays();
        dragging = false;
        draggingItem = null;
        sourceRegion = null;
        targetRegion = null;
        targetOverlay = null;
        lastEventData = null;
    }

    public void Attach(InventoryItemView item)
    {
        if (item == null)
        {
            return;
        }

        item.BeginDrag = BeginDrag;
        item.Dragged = ContinueDrag;
        item.EndDrag = EndDrag;
    }

    public bool RotateCurrent()
    {
        if (!dragging || draggingItem == null)
        {
            return false;
        }

        Vector3 anchorWorldBefore = draggingItem.GetRotationPivotWorldPosition();
        draggingItem.Rotate();
        candidatePlacement = new Placement(
            candidatePlacement.RegionIndex,
            candidatePlacement.Anchor,
            draggingItem.Rotation);
        Vector3 anchorWorldAfter = draggingItem.GetRotationPivotWorldPosition();
        draggingItem.transform.position += anchorWorldBefore - anchorWorldAfter;
        EvaluateCandidate(lastEventData);
        return true;
    }

    private void BeginDrag(InventoryItemView item, PointerEventData eventData)
    {
        if (item == null || item.Entry == null || regions == null)
        {
            return;
        }

        ClearOverlays();
        draggingItem = item;
        inventory = FindInventory(item);
        originalPlacement = item.Entry.Placement;
        candidatePlacement = originalPlacement;
        dragStartMouse = eventData.position;
        lastEventData = eventData;
        dragStartWorldPosition = item.transform.position;
        sourceRegion = FindRegionByIndex(originalPlacement.RegionIndex);
        targetRegion = sourceRegion;
        targetOverlay = sourceRegion != null ? sourceRegion.WarningOverlay : null;
        dragging = sourceRegion != null && inventory != null;
        candidateValid = false;

        if (!dragging)
        {
            return;
        }

        originalPivotScreen = RectTransformUtility.WorldToScreenPoint(
            eventData.pressEventCamera,
            draggingItem.GetRotationPivotWorldPosition());
        ShowOriginalShadow();
        EvaluateCandidate(eventData);
    }

    private void ContinueDrag(InventoryItemView item, PointerEventData eventData)
    {
        if (!dragging || item != draggingItem)
        {
            return;
        }

        draggingItem.transform.position = dragStartWorldPosition +
            (Vector3)(eventData.position - dragStartMouse);
        lastEventData = eventData;
        EvaluateCandidate(eventData);
    }

    private void EndDrag(InventoryItemView item, PointerEventData eventData)
    {
        if (!dragging || item != draggingItem)
        {
            ClearOverlays();
            return;
        }

        if (candidateValid && inventory != null)
        {
            inventory.Runtime.TrySetLocalPlacement(draggingItem.Entry.InstanceId, candidatePlacement);
            inventory.RequestPlacement(
                draggingItem.Entry.InstanceId,
                candidatePlacement.RegionIndex,
                candidatePlacement.Anchor.X,
                candidatePlacement.Anchor.Y,
                candidatePlacement.Rotation);
        }
        else
        {
            inventory.Runtime.TrySetLocalPlacement(
                draggingItem.Entry.InstanceId,
                originalPlacement);
            RestoreOriginalVisual();
        }

        ClearOverlays();
        dragging = false;
        draggingItem = null;
        sourceRegion = null;
        targetRegion = null;
        targetOverlay = null;
        lastEventData = null;
    }

    private void EvaluateCandidate(PointerEventData eventData)
    {
        if (!dragging || draggingItem == null || targetRegion == null || eventData == null)
        {
            candidateValid = false;
            return;
        }

        BackpackGridView hoveredRegion = FindRegion(eventData.position);
        if (hoveredRegion == null)
        {
            candidateValid = false;
            if (targetOverlay != null)
            {
                targetOverlay.ClearPreview();
            }
            return;
        }
        if (hoveredRegion != targetRegion)
        {
            if (targetOverlay != null)
            {
                targetOverlay.ClearPreview();
            }
            targetRegion = hoveredRegion;
            targetOverlay = targetRegion.WarningOverlay;
            targetRegion.WarningOverlay.Configure(targetRegion.CoordinateMap);
        }

        Vector2 pivotScreen = originalPivotScreen + eventData.position - dragStartMouse;
        StorageShapeModule shape = FindShape(draggingItem.Entry.Item);
        Vector2Int anchorOffset = InventoryGeometry.RotationAnchorOffset(
            shape,
            draggingItem.Rotation);
        Vector2 logicalAnchorScreen = pivotScreen + new Vector2(
            anchorOffset.x * targetRegion.CoordinateMap.Step,
            -anchorOffset.y * targetRegion.CoordinateMap.Step);

        if (!targetRegion.CoordinateMap.TryScreenToCell(
            targetRegion.transform as RectTransform,
            logicalAnchorScreen,
            eventData.pressEventCamera,
            out Vector2Int anchor))
        {
            candidateValid = false;
            return;
        }

        candidatePlacement = new Placement(
            targetRegion.RegionIndex,
            new Cell(anchor.x, anchor.y),
            draggingItem.Rotation);
        InventoryPlacementPreview preview = new InventoryPlacementPreview();
        preview.Evaluate(
            FindShape(draggingItem.Entry.Item),
            candidatePlacement,
            targetRegion.Region,
            inventory.Runtime.Entries,
            draggingItem.Entry.InstanceId);
        candidateValid = preview.IsValid;
        if (targetOverlay != null)
        {
            Color color = candidateValid
                ? targetRegion.AllowedPreviewColor
                : targetRegion.WarningPreviewColor;
            targetOverlay.ShowPreview(
                preview.Cells,
                preview.InvalidCells,
                color,
                targetRegion.WarningPreviewColor);
        }
    }

    private void ShowOriginalShadow()
    {
        StorageShapeModule shape = FindShape(draggingItem.Entry.Item);
        if (shape == null || sourceRegion == null)
        {
            return;
        }

        List<Vector2Int> cells = new List<Vector2Int>();
        IReadOnlyList<Vector2Int> occupied = InventoryGeometry.Occupied(shape, originalPlacement.Rotation);
        for (int i = 0; i < occupied.Count; i++)
        {
            cells.Add(occupied[i] + new Vector2Int(originalPlacement.Anchor.X, originalPlacement.Anchor.Y));
        }

        sourceRegion.WarningOverlay.ShowOriginal(
            cells,
            sourceRegion.OriginalShadowColor);
    }

    private void ClearOverlays()
    {
        if (regions == null)
        {
            return;
        }

        for (int i = 0; i < regions.Count; i++)
        {
            BackpackGridView region = regions[i];
            if (region != null && region.WarningOverlay != null)
            {
                region.WarningOverlay.Clear();
            }
        }
    }

    private void RestoreOriginalVisual()
    {
        if (draggingItem == null || sourceRegion == null)
        {
            return;
        }

        draggingItem.transform.SetParent(sourceRegion.ItemLayer, false);
        draggingItem.RestoreVisualPlacement(sourceRegion.CoordinateMap);
        draggingItem.gameObject.SetActive(true);
    }

    private void OnDisable()
    {
        CancelDrag();
    }

    private BackpackGridView FindRegion(Vector2 screenPosition)
    {
        if (regions == null)
        {
            return null;
        }

        for (int i = 0; i < regions.Count; i++)
        {
            BackpackGridView view = regions[i];
            if (view != null && view.gameObject.activeInHierarchy &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    view.transform as RectTransform,
                    screenPosition))
            {
                return view;
            }
        }

        return null;
    }

    private BackpackGridView FindRegionByIndex(int index)
    {
        if (regions == null)
        {
            return null;
        }

        for (int i = 0; i < regions.Count; i++)
        {
            if (regions[i] != null && regions[i].RegionIndex == index)
            {
                return regions[i];
            }
        }

        return null;
    }

    private InventorySystem FindInventory(InventoryItemView item)
    {
        return GetComponentInParent<InventoryWindowUI>() != null
            ? GetComponentInParent<InventoryWindowUI>().Inventory
            : null;
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
