using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryInteractionOverlay : MonoBehaviour
{
    private readonly List<Image> cells = new();
    public void Show(IEnumerable<Vector2Int> positions, InventoryRegionCoordinateMap coordinateMap, Color color)
    {
        Clear();
        foreach (var p in positions)
        {
            var image = new GameObject("PreviewCell", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(transform, false); image.color = color;
            image.raycastTarget = false;
            var r=image.rectTransform; r.anchorMin=r.anchorMax=new Vector2(0,1); r.pivot=new Vector2(0,1);
            r.anchoredPosition = coordinateMap.CellToLocal(p);
            r.sizeDelta = new Vector2(coordinateMap.CellSize, coordinateMap.CellSize);
            cells.Add(image);
        }
    }
    public void Clear() { foreach (var c in cells) if (c) Destroy(c.gameObject); cells.Clear(); }
}
