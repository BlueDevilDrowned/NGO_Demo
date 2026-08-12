using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDatabaseSO))]
public sealed class WeaponDatabaseSOEditor : Editor
{
    private const float IdWidth = 72f;
    private const float RemoveWidth = 24f;

    private SerializedProperty weaponsProperty;

    private void OnEnable()
    {
        weaponsProperty = serializedObject.FindProperty("weapons");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Weapons", EditorStyles.boldLabel);
        DrawDuplicateSummary();
        DrawHeader();
        DrawSortedRows();

        if (GUILayout.Button("Add Weapon"))
            weaponsProperty.arraySize++;

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawDuplicateSummary()
    {
        Dictionary<ushort, List<WeaponSO>> duplicates = GetDuplicateGroups();
        if (duplicates.Count == 0)
            return;

        var ids = new List<ushort>(duplicates.Keys);
        ids.Sort();

        var messages = new List<string>();
        for (int i = 0; i < ids.Count; i++)
        {
            ushort id = ids[i];
            List<WeaponSO> definitions = duplicates[id];
            definitions.Sort(CompareByName);
            messages.Add($"ID {id}: {string.Join(", ", definitions.ConvertAll(item => item.name))}");
        }

        EditorGUILayout.HelpBox(
            "Duplicate weapon IDs:\n" + string.Join("\n", messages),
            MessageType.Error);
    }

    private void DrawHeader()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Weapon", EditorStyles.miniBoldLabel);
            EditorGUILayout.LabelField("ID", EditorStyles.miniBoldLabel, GUILayout.Width(IdWidth));
            GUILayout.Space(RemoveWidth);
        }
    }

    private void DrawSortedRows()
    {
        List<Row> rows = BuildSortedRows();
        HashSet<ushort> duplicateIds = GetDuplicateIds();

        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            SerializedProperty element = weaponsProperty.GetArrayElementAtIndex(row.ArrayIndex);

            Color oldColor = GUI.backgroundColor;
            if (row.Weapon != null && duplicateIds.Contains(row.Weapon.Id))
                GUI.backgroundColor = new Color(1f, 0.55f, 0.55f);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PropertyField(element, GUIContent.none);
                DrawWeaponId(row.Weapon);

                if (GUILayout.Button("-", GUILayout.Width(RemoveWidth)))
                {
                    RemoveAt(row.ArrayIndex);
                    GUI.backgroundColor = oldColor;
                    break;
                }
            }

            GUI.backgroundColor = oldColor;
        }
    }

    private static void DrawWeaponId(WeaponSO weapon)
    {
        if (weapon == null)
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.IntField(0, GUILayout.Width(IdWidth));
            return;
        }

        var weaponObject = new SerializedObject(weapon);
        SerializedProperty idProperty = weaponObject.FindProperty("id");
        weaponObject.Update();

        EditorGUI.BeginChangeCheck();
        int value = EditorGUILayout.IntField(idProperty.intValue, GUILayout.Width(IdWidth));
        if (EditorGUI.EndChangeCheck())
        {
            idProperty.intValue = Mathf.Clamp(value, 1, ushort.MaxValue);
            weaponObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(weapon);
        }
    }

    private List<Row> BuildSortedRows()
    {
        var rows = new List<Row>(weaponsProperty.arraySize);
        for (int i = 0; i < weaponsProperty.arraySize; i++)
        {
            WeaponSO weapon = weaponsProperty
                .GetArrayElementAtIndex(i)
                .objectReferenceValue as WeaponSO;
            rows.Add(new Row(i, weapon));
        }

        rows.Sort(CompareRows);
        return rows;
    }

    private Dictionary<ushort, List<WeaponSO>> GetDuplicateGroups()
    {
        var groups = new Dictionary<ushort, List<WeaponSO>>();
        for (int i = 0; i < weaponsProperty.arraySize; i++)
        {
            WeaponSO weapon = weaponsProperty
                .GetArrayElementAtIndex(i)
                .objectReferenceValue as WeaponSO;
            if (weapon == null)
                continue;

            if (!groups.TryGetValue(weapon.Id, out List<WeaponSO> definitions))
            {
                definitions = new List<WeaponSO>();
                groups.Add(weapon.Id, definitions);
            }

            definitions.Add(weapon);
        }

        var duplicates = new Dictionary<ushort, List<WeaponSO>>();
        foreach (KeyValuePair<ushort, List<WeaponSO>> pair in groups)
        {
            if (pair.Value.Count > 1)
                duplicates.Add(pair.Key, pair.Value);
        }

        return duplicates;
    }

    private HashSet<ushort> GetDuplicateIds()
    {
        return new HashSet<ushort>(GetDuplicateGroups().Keys);
    }

    private void RemoveAt(int index)
    {
        SerializedProperty element = weaponsProperty.GetArrayElementAtIndex(index);
        element.objectReferenceValue = null;
        weaponsProperty.DeleteArrayElementAtIndex(index);
    }

    private static int CompareRows(Row left, Row right)
    {
        if (left.Weapon == null)
            return right.Weapon == null ? left.ArrayIndex.CompareTo(right.ArrayIndex) : 1;
        if (right.Weapon == null)
            return -1;

        int idComparison = left.Weapon.Id.CompareTo(right.Weapon.Id);
        return idComparison != 0
            ? idComparison
            : CompareByName(left.Weapon, right.Weapon);
    }

    private static int CompareByName(WeaponSO left, WeaponSO right)
    {
        return string.Compare(left.name, right.name, StringComparison.Ordinal);
    }

    private readonly struct Row
    {
        public readonly int ArrayIndex;
        public readonly WeaponSO Weapon;

        public Row(int arrayIndex, WeaponSO weapon)
        {
            ArrayIndex = arrayIndex;
            Weapon = weapon;
        }
    }
}
