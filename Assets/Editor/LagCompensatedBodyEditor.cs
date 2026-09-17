using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LagCompensatedBody))]
public sealed class LagCompensatedBodyEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        EditorGUILayout.Space();
        LagCompensatedBody body = (LagCompensatedBody)target;
        EditorGUILayout.LabelField("Offline Rewind Tree", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Body type", body.BodyType.ToString());
        EditorGUILayout.LabelField("Cached colliders", body.Nodes.Count.ToString());

        if (GUILayout.Button("Rebuild Offline Collider Tree", GUILayout.Height(28)))
        {
            Undo.RecordObject(body, "Rebuild Offline Collider Tree");
            body.RebuildOfflineTree();
            EditorUtility.SetDirty(body);
            PrefabUtility.RecordPrefabInstancePropertyModifications(body);
        }

        if (body.Nodes.Count == 0)
            EditorGUILayout.HelpBox("Build the tree after adding or changing child colliders.", MessageType.Warning);
        serializedObject.ApplyModifiedProperties();
    }
}
