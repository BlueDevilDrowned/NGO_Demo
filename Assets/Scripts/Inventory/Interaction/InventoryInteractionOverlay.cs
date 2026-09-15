using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryInteractionOverlay : MonoBehaviour
{
    private readonly List<Image> previewCells = new List<Image>();
    private readonly List<Image> invalidPreviewCells = new List<Image>();
    private readonly List<Image> originalCells = new List<Image>();
    private InventoryRegionCoordinateMap metrics;

    public void Configure(InventoryRegionCoordinateMap coordinateMap)
    {
        metrics = coordinateMap;
    }

    public void ShowOriginal(IEnumerable<Vector2Int> positions, Color color)
    {
        Render(positions, color, originalCells);
    }

    public void ShowPreview(IEnumerable<Vector2Int> positions, Color color)
    {
        Render(positions, color, previewCells);
        SetInactive(invalidPreviewCells);
    }

    public void ShowPreview(
        IEnumerable<Vector2Int> positions,
        IEnumerable<Vector2Int> invalidPositions,
        Color validColor,
        Color invalidColor)
    {
        HashSet<Vector2Int> invalid = new HashSet<Vector2Int>();
        if (invalidPositions != null)
        {
            foreach (Vector2Int position in invalidPositions)
            {
                invalid.Add(position);
            }
        }

        List<Vector2Int> validPositions = new List<Vector2Int>();
        List<Vector2Int> invalidList = new List<Vector2Int>();
        if (positions != null)
        {
            foreach (Vector2Int position in positions)
            {
                if (invalid.Contains(position))
                {
                    invalidList.Add(position);
                }
                else
                {
                    validPositions.Add(position);
                }
            }
        }

        Render(validPositions, validColor, previewCells);
        Render(invalidList, invalidColor, invalidPreviewCells);
    }

    public void ClearPreview()
    {
        SetInactive(previewCells);
        SetInactive(invalidPreviewCells);
    }

    public void Clear()
    {
        SetInactive(previewCells);
        SetInactive(invalidPreviewCells);
        SetInactive(originalCells);
    }

    private void Render(IEnumerable<Vector2Int> positions, Color color, List<Image> pool)
    {
        int index = 0;
        if (positions != null)
        {
            foreach (Vector2Int position in positions)
            {
                Image image = GetOrCreate(pool, index);
                image.gameObject.SetActive(true);
                image.color = color;
                image.rectTransform.anchoredPosition = metrics.CellCenter(position);
                image.rectTransform.sizeDelta = new Vector2(metrics.CellSize, metrics.CellSize);
                index++;
            }
        }

        for (int i = index; i < pool.Count; i++)
        {
            pool[i].gameObject.SetActive(false);
        }
    }

    private Image GetOrCreate(List<Image> pool, int index)
    {
        if (index < pool.Count)
        {
            return pool[index];
        }

        GameObject cellObject = new GameObject("PreviewCell", typeof(RectTransform), typeof(Image));
        cellObject.transform.SetParent(transform, false);
        Image image = cellObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.rectTransform.anchorMin = new Vector2(0f, 1f);
        image.rectTransform.anchorMax = new Vector2(0f, 1f);
        image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        pool.Add(image);
        return image;
    }

    private static void SetInactive(List<Image> pool)
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null)
            {
                pool[i].gameObject.SetActive(false);
            }
        }
    }
}
