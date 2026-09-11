using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BackpackGridView : MonoBehaviour
{
    [SerializeField] private RectTransform regionRoot;
    [SerializeField] private GridLayoutGroup cellLayer;
    [SerializeField] private Image cellPrefab;
    [SerializeField] private Color enabledColor = new Color(1f, 1f, 1f, .12f);
    [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, .025f);
    private readonly List<Image> cells = new();

    public void Build(InventoryRegionDefinition definition, float cellSize, float spacing)
    {
        if (definition == null) return;
        cellLayer ??= GetComponentInChildren<GridLayoutGroup>();
        if (cellLayer == null)
        {
            var layer = new GameObject("Cells", typeof(RectTransform), typeof(GridLayoutGroup));
            layer.transform.SetParent(transform, false);
            cellLayer = layer.GetComponent<GridLayoutGroup>();
        }
        if (cellPrefab == null)
        {
            var fallback = new GameObject("Cell", typeof(RectTransform), typeof(Image));
            cellPrefab = fallback.GetComponent<Image>();
            fallback.SetActive(false);
        }
        cellLayer.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        cellLayer.constraintCount = definition.Width;
        cellLayer.cellSize = new Vector2(cellSize, cellSize);
        cellLayer.spacing = new Vector2(spacing, spacing);
        int count = definition.Width * definition.Height;
        while (cells.Count < count) cells.Add(Instantiate(cellPrefab, cellLayer.transform));
        var enabled = new HashSet<Vector2Int>(definition.EnabledCells ?? new List<Vector2Int>());
        for (int i = 0; i < cells.Count; i++)
        {
            bool active = i < count;
            cells[i].gameObject.SetActive(active);
            if (!active) continue;
            int x = i % definition.Width;
            int y = i / definition.Width;
            cells[i].color = enabled.Contains(new Vector2Int(x, y)) ? enabledColor : disabledColor;
        }
    }
}
