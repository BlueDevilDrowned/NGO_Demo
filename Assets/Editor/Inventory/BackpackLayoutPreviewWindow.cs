using UnityEditor;
using UnityEngine;

public sealed class BackpackLayoutPreviewWindow : EditorWindow
{
    private InventoryItemDefinition item;
    private BackpackModule backpack;
    private int dragging = -1;
    private Vector2 dragOffset;
    private Rect dragStartRect;
    private const float PreviewSize = 520f;

    public static void Open(InventoryItemDefinition item, BackpackModule backpack)
    {
        var window = GetWindow<BackpackLayoutPreviewWindow>("背包布局预览");
        window.item = item;
        window.backpack = backpack;
        window.minSize = new Vector2(620f, 620f);
        window.Show();
    }

    private void OnGUI()
    {
        if (backpack == null || backpack.Regions == null) return;
        EditorGUILayout.LabelField(item != null ? item.DisplayName : "背包", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("拖动区域矩形调整排版。位置和尺寸使用 0~1 归一化坐标。", MessageType.Info);
        if (GUILayout.Button("自动填充布局")) AutoPack();
        Rect preview = GUILayoutUtility.GetRect(PreviewSize, PreviewSize, GUILayout.ExpandWidth(false));
        EditorGUI.DrawRect(preview, new Color(.08f, .08f, .08f));
        Event current = Event.current;
        for (int i = 0; i < backpack.Regions.Count; i++)
        {
            InventoryRegionDefinition region = backpack.Regions[i];
            if (region == null) continue;
            Rect normalized = region.NormalizedRect;
            Rect rect = new Rect(preview.x + normalized.x * preview.width,
                preview.y + (1f - normalized.y - normalized.height) * preview.height,
                normalized.width * preview.width, normalized.height * preview.height);
            EditorGUI.DrawRect(rect, new Color(.15f, .45f, .8f, .45f));
            GUI.Box(rect, $"{region.DisplayName}\n{region.Width} × {region.Height}");
            if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
            {
                dragging = i;
                dragOffset = current.mousePosition - rect.position;
                dragStartRect = region.NormalizedRect;
                current.Use();
            }
        }
        if (dragging >= 0 && current.type == EventType.MouseDrag)
        {
            InventoryRegionDefinition region = backpack.Regions[dragging];
            Vector2 position = (current.mousePosition - preview.position - dragOffset) / PreviewSize;
            position.y = 1f - position.y - region.NormalizedRect.height;
            Undo.RecordObject(item, "调整背包区域布局");
            Rect candidate = new Rect(
                Mathf.Clamp(position.x, 0f, 1f - region.NormalizedRect.width),
                Mathf.Clamp(position.y, 0f, 1f - region.NormalizedRect.height),
                region.NormalizedRect.width, region.NormalizedRect.height);
            region.NormalizedRect = ResolveNonOverlappingPosition(dragging, candidate);
            EditorUtility.SetDirty(item);
            Repaint();
            current.Use();
        }
        if (current.type == EventType.MouseUp) dragging = -1;
        EditorGUILayout.Space(8);
    }

    private void AutoPack()
    {
        Undo.RecordObject(item, "自动排列背包区域");
        int count = backpack.Regions.Count;
        int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
        int rows = Mathf.CeilToInt((float)count / columns);
        float gap = 0.02f;
        float available = 1f - gap * (columns + 1);
        float availableHeight = 1f - gap * (rows + 1);
        float[] columnUnits = new float[columns];
        float[] rowUnits = new float[rows];
        for (int i = 0; i < count; i++)
        {
            int column = i % columns;
            int row = i / columns;
            var region = backpack.Regions[i];
            columnUnits[column] = Mathf.Max(columnUnits[column], region.Width);
            rowUnits[row] = Mathf.Max(rowUnits[row], region.Height);
        }
        float unit = Mathf.Min(available / Mathf.Max(1f, Sum(columnUnits)),
            availableHeight / Mathf.Max(1f, Sum(rowUnits)));
        float[] columnPositions = new float[columns];
        float[] rowPositions = new float[rows];
        for (int i = 1; i < columns; i++) columnPositions[i] = columnPositions[i - 1] + columnUnits[i - 1] * unit + gap;
        for (int i = 1; i < rows; i++) rowPositions[i] = rowPositions[i - 1] + rowUnits[i - 1] * unit + gap;
        for (int i = 0; i < count; i++)
        {
            int x = i % columns;
            int y = i / columns;
            var region = backpack.Regions[i];
            backpack.Regions[i].NormalizedRect = new Rect(
                gap + columnPositions[x], 1f - gap - rowPositions[y] - region.Height * unit,
                region.Width * unit, region.Height * unit);
        }
        EditorUtility.SetDirty(item);
        Repaint();
    }

    private static float Sum(float[] values)
    {
        float result = 0f;
        for (int i = 0; i < values.Length; i++) result += values[i];
        return result;
    }

    private Rect ResolveNonOverlappingPosition(int movingIndex, Rect candidate)
    {
        const float epsilon = 0.0001f;
        candidate.x = Mathf.Clamp(candidate.x, 0f, 1f - candidate.width);
        candidate.y = Mathf.Clamp(candidate.y, 0f, 1f - candidate.height);
        for (int i = 0; i < backpack.Regions.Count; i++)
        {
            if (i == movingIndex || backpack.Regions[i] == null) continue;
            Rect other = backpack.Regions[i].NormalizedRect;
            if (!candidate.Overlaps(other)) continue;
            float pushLeft = candidate.xMax - other.xMin;
            float pushRight = other.xMax - candidate.xMin;
            float pushDown = candidate.yMax - other.yMin;
            float pushUp = other.yMax - candidate.yMin;
            float horizontal = Mathf.Min(pushLeft, pushRight);
            float vertical = Mathf.Min(pushDown, pushUp);
            if (horizontal <= vertical)
                candidate.x += candidate.center.x < other.center.x ? -horizontal - epsilon : horizontal + epsilon;
            else
                candidate.y += candidate.center.y < other.center.y ? -vertical - epsilon : vertical + epsilon;
            candidate.x = Mathf.Clamp(candidate.x, 0f, 1f - candidate.width);
            candidate.y = Mathf.Clamp(candidate.y, 0f, 1f - candidate.height);
        }
        return candidate;
    }
}

