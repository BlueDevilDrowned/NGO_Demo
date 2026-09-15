# Inventory UI and Interaction Design

## Coordinate model

- Each backpack region is positioned by `NormalizedRect`.
- `NormalizedRect` uses a bottom-left origin, matching the editor layout tool and serialized backpack data.
- Runtime region views use a top-left pivot, so the region's top edge is placed at `NormalizedRect.yMax`.
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
- The canonical item anchor is the top-left virtual cell of the unrotated shape and is the permanent rotation pivot.
- The placement anchor is the top-left virtual cell of the normalized shape at the current rotation and may move when rotation changes.
- The item root rotates around the canonical pivot; its icon and occupied-cell layer rotate together.
- The icon fills the canonical shape bounding rectangle while preserving aspect ratio, which becomes the rotated bounding rectangle with the root transform.

## Dragging

- On pointer down, record mouse screen position, the canonical pivot screen position, and original authoritative `Placement`.
- Draw original occupied cells in `WarningCells` using a gray shadow.
- During drag, item visual position equals its original screen position plus mouse delta.
- The canonical pivot visual position uses the same delta.
- The rotated top-left virtual anchor is derived from that pivot and snaps to the nearest logical grid cell center.
- The snapped logical anchor and current rotation form the candidate `Placement`.
- Warning cells are drawn from the candidate's actual rotated occupied cells.
- Valid cells use the configured allowed color; overlap, disabled, or out-of-range cells use the warning color.
- Pointer up restores the original placement when invalid.
- Pointer up immediately applies the local valid presentation and queues an `OwnerToServer` placement request when valid.
- The next authoritative `ServerToClients` snapshot is the final state and can roll back the local presentation.
- Items may move between regions.

## Rotation

- Q is routed through `NetWorkPlayerController` to the active inventory input subsystem.
- Rotation changes the current item rotation by 90 degrees.
- Rotation keeps the canonical pivot visual position unchanged and recomputes the logical anchor for the new angle.
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
