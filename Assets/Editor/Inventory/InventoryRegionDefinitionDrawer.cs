using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InventoryRegionDefinition))]
public sealed class InventoryRegionDefinitionDrawer : PropertyDrawer
{
    private const float CellSize = 24f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        int height = Mathf.Clamp(property.FindPropertyRelative("Height").intValue, 1, 32);
        return EditorGUIUtility.singleLineHeight * 2f + height * CellSize + 12f;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        float line = EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, line), property.FindPropertyRelative("DisplayName"), label);
        var width = property.FindPropertyRelative("Width");
        var height = property.FindPropertyRelative("Height");
        int w = Mathf.Clamp(width.intValue, 1, 32), h = Mathf.Clamp(height.intValue, 1, 32);
        width.intValue = EditorGUI.IntField(new Rect(position.x, position.y + line, position.width * .48f, line), "宽度", w);
        height.intValue = EditorGUI.IntField(new Rect(position.x + position.width * .52f, position.y + line, position.width * .48f, line), "高度", h);
        w = Mathf.Clamp(width.intValue, 1, 32); h = Mathf.Clamp(height.intValue, 1, 32);
        var cells = property.FindPropertyRelative("EnabledCells");
        var enabled = ReadCells(cells);
        var grid = new Rect(position.x, position.y + line * 2f + 4f, w * CellSize, h * CellSize);
        for (int y = h - 1; y >= 0; y--)
        for (int x = 0; x < w; x++)
        {
            var rect = new Rect(grid.x + x * CellSize, grid.y + (h - 1 - y) * CellSize, CellSize - 1, CellSize - 1);
            var cell = new Vector2Int(x, y);
            bool active = enabled.Contains(cell);
            if (GUI.Button(rect, active ? "■" : "·"))
            {
                if (active) enabled.Remove(cell); else enabled.Add(cell);
                WriteCells(cells, enabled);
            }
        }
        EditorGUI.EndProperty();
    }

    private static HashSet<Vector2Int> ReadCells(SerializedProperty property)
    {
        var result = new HashSet<Vector2Int>();
        for (int i = 0; i < property.arraySize; i++)
        {
            var value = property.GetArrayElementAtIndex(i);
            result.Add(new Vector2Int(value.FindPropertyRelative("x").intValue, value.FindPropertyRelative("y").intValue));
        }
        return result;
    }

    private static void WriteCells(SerializedProperty property, HashSet<Vector2Int> cells)
    {
        property.arraySize = cells.Count;
        int index = 0;
        foreach (Vector2Int cell in cells)
        {
            var value = property.GetArrayElementAtIndex(index++);
            value.FindPropertyRelative("x").intValue = cell.x;
            value.FindPropertyRelative("y").intValue = cell.y;
        }
    }
}
