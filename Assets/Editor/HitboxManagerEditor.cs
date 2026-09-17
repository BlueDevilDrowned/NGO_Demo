using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HitboxManager))]
public sealed class HitboxManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Hitbox Layer Tools", EditorStyles.boldLabel);
        if (GUILayout.Button("Apply Layer To Hitbox Objects", GUILayout.Height(28)))
        {
            HitboxManager manager = (HitboxManager)target;
            Undo.RegisterFullObjectHierarchyUndo(manager.gameObject, "Apply Hitbox Layer");
            manager.ApplyConfiguredLayer();
            EditorUtility.SetDirty(manager);
            PrefabUtility.RecordPrefabInstancePropertyModifications(manager);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
