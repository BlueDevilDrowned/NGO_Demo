using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WeaponInstance))]
public sealed class WeaponInstanceEditor:Editor
{
    private SerializedProperty modelType;
    private SerializedProperty aimTransform;
    private SerializedProperty aimAxis;
    private SerializedProperty aimUpAxis;
    private SerializedProperty leftHandGrip;

    private void OnEnable()
    {
        modelType=serializedObject.FindProperty("modelType");
        aimTransform=serializedObject.FindProperty("aimTransform");
        aimAxis=serializedObject.FindProperty("aimAxis");
        aimUpAxis=serializedObject.FindProperty("aimUpAxis");
        leftHandGrip=serializedObject.FindProperty("leftHandGrip");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(
            serializedObject.FindProperty("muzzle"));
        EditorGUILayout.PropertyField(modelType);

        WeaponModelType selected=(WeaponModelType)modelType.enumValueIndex;
        bool includesThirdPerson=
            selected==WeaponModelType.Shared||
            selected==WeaponModelType.ThirdPerson;
        if(includesThirdPerson)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Aim",EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(aimTransform);
            EditorGUILayout.PropertyField(aimAxis);
            EditorGUILayout.PropertyField(aimUpAxis);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("IK",EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(leftHandGrip);
        }
        else
        {
            EditorGUILayout.HelpBox(
                "AimTransform, AimAxis, and AimUpAxis are only used by third-person weapon models.",
                MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
