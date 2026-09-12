# Inventory UI and Interaction Design

## Coordinate model

- Each backpack region is positioned by `NormalizedRect`.
- `NormalizedRect` determines the region's top-left position in the backpack design space.
- The logical origin `(0, 0)` is the center of the region's top-left grid cell.
- Cell spacing is configurable and defaults to one pixel.
- All cells, items, warning cells, and drag conversion use one shared `GridMetrics` instance.
- A shared square cell size is calculated once when the inventory window opens, using the short side of the reference region.
- The scaled grid starts at the configured region top-left and expands right and down.

## Region hierarchy

```text
BackpackView
└── RegionView
    ├── NormalCells
    ├── AllowedCells
    ├── WarningCells
    └── Items
```

- Every layer uses the RegionView coordinate space.
- Cells are individual rectangles; no layout group is used.
- Normal and allowed cells are persistent and pooled.
- Warning cells are pooled and only show drag previews, original placement shadows, overlap, or invalid placement.
- Items are pooled by `instanceId`.

## Item model

- Every item is an independent `InventoryItemView`.
- The item view contains its icon and occupied-cell overlays.
- Occupied cells come from `StorageShapeModule.Cells`.
- Rotation is applied to logical shape cells before drawing or validation.
- The item anchor is the top-left logical anchor cell; a virtual anchor is allowed when that cell is not occupied.
- The icon fills the rotated shape bounding rectangle while preserving aspect ratio.

## Dragging

- On pointer down, record mouse screen position, item screen position, and original authoritative `Placement`.
- Draw original occupied cells in `WarningCells` using a gray shadow.
- During drag, item visual position equals its original screen position plus mouse delta.
- The anchor visual position uses the same delta and snaps to the nearest logical grid cell center.
- The snapped anchor and current rotation form the candidate `Placement`.
- Warning cells are drawn from the candidate's actual rotated occupied cells.
- Valid cells use the configured allowed color; overlap, disabled, or out-of-range cells use the warning color.
- Pointer up restores the original placement when invalid.
- Pointer up immediately applies the local valid presentation and queues an `OwnerToServer` placement request when valid.
- The next authoritative `ServerToClients` snapshot is the final state and can roll back the local presentation.
- Items may move between regions.

## Rotation

- Q is routed through `NetWorkPlayerController` to the active inventory input subsystem.
- Rotation changes the current item rotation by 90 degrees.
- Rotation keeps the mouse anchor visual position unchanged.
- The candidate occupied cells and warning layer are rebuilt after rotation.

## Input

- `NetWorkPlayerController` owns an input subsystem manager.
- `GameplayInputSubsystem` is the main subsystem.
- `InventoryInputSubsystem` is activated while the inventory window is open.
- Closing any window restores the main subsystem.
- Gameplay movement inputs may remain enabled while inventory is open according to policy.

## Synchronization

- Inventory state is authoritative on the server.
- `InventorySnapshot` contains item IDs, instance IDs, and placements.
- `InventoryPlacementRequestChannel` uses `OwnerToServer` and sends only when dirty.
- The server validates ownership, region, rotation, enabled cells, bounds, and overlap.
- The server updates `InventoryRuntime`; the existing server-to-client snapshot distributes the result.

## Refresh and pooling

- Opening the window calculates metrics and builds the static region structure.
- Resolution changes are handled on the next open.
- Refresh updates only changed entries.
- Unused cell, warning, and item views are deactivated and retained for reuse.
- No per-frame layout rebuild and no full-window destruction are allowed.
