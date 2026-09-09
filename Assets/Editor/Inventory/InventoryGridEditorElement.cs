using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>共享网格：在整个网格上捕获指针，以实际鼠标坐标计算选区。</summary>
public sealed class InventoryGridEditorElement : VisualElement
{
    public const int CellSize = 32;
    private readonly Func<int> getWidth, getHeight;
    private readonly Func<HashSet<Vector2Int>> getCells;
    private readonly Action<Vector2Int, Vector2Int> invert;
    private readonly List<Label> views = new List<Label>();
    private int width, height, pointer = -1;
    private Vector2Int start, end;

    public InventoryGridEditorElement(Func<int> getWidth, Func<int> getHeight,
        Func<HashSet<Vector2Int>> getCells, Action<Vector2Int, Vector2Int> invert)
    {
        this.getWidth = getWidth; this.getHeight = getHeight;
        this.getCells = getCells; this.invert = invert;
        focusable = true;
        style.flexShrink = 0;
        RegisterCallback<PointerDownEvent>(e =>
        {
            if (pointer >= 0 || (e.button != 0 && e.button != 1)) return;
            Focus();
            Vector2Int cell = CellAt(e.position);
            if (e.button == 0) { invert(cell, cell); Paint(); }
            else { start = end = cell; pointer = e.pointerId; this.CapturePointer(pointer); Paint(); }
            e.StopPropagation();
        });
        RegisterCallback<PointerMoveEvent>(e =>
        {
            if (pointer != e.pointerId) return;
            end = CellAt(e.position); Paint(); e.StopPropagation();
        });
        RegisterCallback<PointerUpEvent>(e =>
        {
            if (e.button != 1 || pointer != e.pointerId) return;
            Vector2Int from = start, to = CellAt(e.position);
            Cancel(); invert(from, to); Paint(); e.StopPropagation();
        });
        RegisterCallback<PointerCaptureOutEvent>(e => { pointer = -1; Paint(); });
        RegisterCallback<PointerCancelEvent>(e => Cancel());
        RegisterCallback<DetachFromPanelEvent>(e => Cancel());
        RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { Cancel(); e.StopPropagation(); } });
        Rebuild();
    }

    public void Rebuild()
    {
        Cancel(); Clear(); views.Clear();
        width = Mathf.Max(1, getWidth()); height = Mathf.Max(1, getHeight());
        style.width = width * CellSize; style.height = height * CellSize;
        for (int row = 0; row < height; row++)
        for (int x = 0; x < width; x++)
        {
            var view = new Label { pickingMode = PickingMode.Ignore };
            view.style.position = Position.Absolute;
            view.style.left = x * CellSize; view.style.top = row * CellSize;
            view.style.width = view.style.height = CellSize;
            view.style.unityTextAlign = TextAnchor.MiddleCenter;
            view.style.borderLeftWidth = view.style.borderRightWidth = 2;
            view.style.borderTopWidth = view.style.borderBottomWidth = 2;
            Add(view); views.Add(view);
        }
        Paint();
    }
    private Vector2Int CellAt(Vector3 position)
    {
        Vector2 local = this.WorldToLocal(new Vector2(position.x, position.y));
        return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(local.x / CellSize), 0, width - 1),
            height - 1 - Mathf.Clamp(Mathf.FloorToInt(local.y / CellSize), 0, height - 1));
    }
    private void Cancel()
    {
        int old = pointer; pointer = -1;
        if (old >= 0 && this.HasPointerCapture(old)) this.ReleasePointer(old);
        Paint();
    }
    private void Paint()
    {
        if (views.Count == 0) return;
        var cells = getCells();
        for (int i = 0; i < views.Count; i++)
        {
            int x = i % width, y = height - 1 - i / width;
            bool active = cells.Contains(new Vector2Int(x, y));
            bool selected = pointer >= 0 && x >= Mathf.Min(start.x, end.x) && x <= Mathf.Max(start.x, end.x)
                && y >= Mathf.Min(start.y, end.y) && y <= Mathf.Max(start.y, end.y);
            var view = views[i]; view.text = active ? "■" : "·";
            view.style.color = Color.white;
            view.style.backgroundColor = active ? new Color(.15f, .45f, .7f) : new Color(.2f, .2f, .2f);
            Color border = selected ? new Color(1f, .65f, .1f) : new Color(.3f, .3f, .3f);
            view.style.borderLeftColor = view.style.borderRightColor = border;
            view.style.borderTopColor = view.style.borderBottomColor = border;
        }
    }
}
