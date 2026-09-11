using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponDatabaseSO))]
public sealed class WeaponDatabaseSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty weapons = serializedObject.FindProperty("weapons");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemRegistry"));
        EditorGUILayout.LabelField("Weapons", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("ID = 注册位置 + 1。清空位置不会改变后续武器的 ID。", MessageType.Info);
        var seen = new HashSet<WeaponSO>();
        for (int index = 0; index < weapons.arraySize; index++)
        {
            SerializedProperty element = weapons.GetArrayElementAtIndex(index);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField((index + 1).ToString(), GUILayout.Width(72f));
                EditorGUILayout.PropertyField(element, GUIContent.none);
                if (GUILayout.Button("Clear", GUILayout.Width(48f)))
                    element.objectReferenceValue = null;
            }
            var weapon = element.objectReferenceValue as WeaponSO;
            if (weapon != null && !seen.Add(weapon))
                EditorGUILayout.HelpBox($"武器重复注册：{weapon.name}", MessageType.Error);
        }
        using (new EditorGUI.DisabledScope(weapons.arraySize >= ushort.MaxValue))
        {
            if (GUILayout.Button("Add Weapon"))
            {
                int index = weapons.arraySize++;
                weapons.GetArrayElementAtIndex(index).objectReferenceValue = null;
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
