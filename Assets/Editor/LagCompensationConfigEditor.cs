using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LagCompensationConfig))]
public sealed class LagCompensationConfigEditor : Editor
{
    private SerializedProperty captureIntervalSeconds;
    private SerializedProperty historyDurationSeconds;
    private SerializedProperty staticCollisionMask;
    private SerializedProperty rewindProxyLayerName;
    private SerializedProperty raycastBufferSize;
    private SerializedProperty enableDebugDraw;
    private SerializedProperty debugDrawDurationSeconds;
    private SerializedProperty clearDebugOnNextShot;
    private SerializedProperty maxDebugShots;
    private SerializedProperty drawDebugTickLabels;

    private void OnEnable()
    {
        captureIntervalSeconds = serializedObject.FindProperty("captureIntervalSeconds");
        historyDurationSeconds = serializedObject.FindProperty("historyDurationSeconds");
        staticCollisionMask = serializedObject.FindProperty("staticCollisionMask");
        rewindProxyLayerName = serializedObject.FindProperty("rewindProxyLayerName");
        raycastBufferSize = serializedObject.FindProperty("raycastBufferSize");
        enableDebugDraw = serializedObject.FindProperty("enableDebugDraw");
        debugDrawDurationSeconds = serializedObject.FindProperty("debugDrawDurationSeconds");
        clearDebugOnNextShot = serializedObject.FindProperty("clearDebugOnNextShot");
        maxDebugShots = serializedObject.FindProperty("maxDebugShots");
        drawDebugTickLabels = serializedObject.FindProperty("drawDebugTickLabels");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(captureIntervalSeconds);
        EditorGUILayout.PropertyField(historyDurationSeconds);
        EditorGUILayout.PropertyField(staticCollisionMask);

        int currentLayer = LayerMask.NameToLayer(rewindProxyLayerName.stringValue);
        EditorGUI.BeginChangeCheck();
        int selectedLayer = EditorGUILayout.LayerField(
            new GUIContent("Rewind Proxy Layer"),
            Mathf.Max(0, currentLayer));
        if (EditorGUI.EndChangeCheck())
        {
            rewindProxyLayerName.stringValue = LayerMask.LayerToName(selectedLayer);
            currentLayer = selectedLayer;
        }

        if (currentLayer < 0)
        {
            EditorGUILayout.HelpBox(
                $"Layer '{rewindProxyLayerName.stringValue}' does not exist.",
                MessageType.Error);
        }

        EditorGUILayout.PropertyField(raycastBufferSize);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Draw", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableDebugDraw);
        if (enableDebugDraw.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(debugDrawDurationSeconds);
            EditorGUILayout.PropertyField(clearDebugOnNextShot);
            EditorGUILayout.PropertyField(maxDebugShots);
            EditorGUILayout.PropertyField(drawDebugTickLabels);
            EditorGUI.indentLevel--;
        }
        serializedObject.ApplyModifiedProperties();
    }
}
