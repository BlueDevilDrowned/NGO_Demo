using System;
using System.Reflection;
using Animancer;
using UnityEditor;
using UnityEngine;

public static class RootMotionBakeBatch
{
    private const string SamplePrefabPath="Assets/Prefab/Player0.prefab";
    private const string LeftTransitionPath="Assets/Animation/First/Idle/Turn L.asset";
    private const string RightTransitionPath="Assets/Animation/First/Idle/Turn R.asset";
    private const string LeftDataPath="Assets/Config/RootData/FirstPerson/Turn/TurnLeft.asset";
    private const string RightDataPath="Assets/Config/RootData/FirstPerson/Turn/TurnRight.asset";

    public static void BakeFirstPersonTurns()
    {
        AssetDatabase.Refresh();
        GameObject samplePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(SamplePrefabPath);
        Bake(LeftDataPath,LeftTransitionPath,samplePrefab);
        Bake(RightDataPath,RightTransitionPath,samplePrefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Baked first-person turn root motion data.");
    }

    private static void Bake(string dataPath,string transitionPath,GameObject samplePrefab)
    {
        if(samplePrefab==null)
            throw new InvalidOperationException($"Missing sample prefab: {SamplePrefabPath}");

        TransitionAsset transition=AssetDatabase.LoadAssetAtPath<TransitionAsset>(transitionPath);
        if(transition==null)
            throw new InvalidOperationException($"Missing transition asset: {transitionPath}");

        RootMotionData data=AssetDatabase.LoadAssetAtPath<RootMotionData>(dataPath);
        if(data==null)
        {
            string folder=System.IO.Path.GetDirectoryName(dataPath).Replace('\\','/');
            EnsureFolder(folder);
            data=ScriptableObject.CreateInstance<RootMotionData>();
            AssetDatabase.CreateAsset(data,dataPath);
        }

        if(!AnimationBakerEditorUtility.TryResolveClip(transition,out AnimationClip clip,out string error))
            throw new InvalidOperationException(error);
        if(clip==null)
            throw new InvalidOperationException($"Transition has no clip: {transitionPath}");

        SerializedObject serializedData=new SerializedObject(data);
        serializedData.FindProperty("_clip").objectReferenceValue=clip;
        serializedData.ApplyModifiedPropertiesWithoutUndo();

        RootMotionBakerWindow baker=ScriptableObject.CreateInstance<RootMotionBakerWindow>();
        SetField(baker,"_targetData",data);
        SetField(baker,"_samplePrefab",samplePrefab);
        SetField(baker,"_animationSource",transition);
        SetField(baker,"_sampleRateMode",Enum.Parse(GetFieldType(baker,"_sampleRateMode"),"Fps60"));
        SetField(baker,"_rotationTolerance",0.5f);

        try
        {
            MethodInfo bakeClip=typeof(RootMotionBakerWindow).GetMethod(
                "BakeClip",BindingFlags.Instance|BindingFlags.NonPublic);
            bakeClip.Invoke(baker,null);
            EditorUtility.SetDirty(data);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(baker);
            EditorUtility.ClearProgressBar();
        }
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts=folder.Split('/');
        string current=parts[0];
        for(int i=1;i<parts.Length;i++)
        {
            string next=$"{current}/{parts[i]}";
            if(!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current,parts[i]);
            current=next;
        }
    }

    private static Type GetFieldType(object instance,string fieldName)
    {
        return instance.GetType().GetField(
            fieldName,BindingFlags.Instance|BindingFlags.NonPublic).FieldType;
    }

    private static void SetField(object instance,string fieldName,object value)
    {
        instance.GetType().GetField(
            fieldName,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(instance,value);
    }
}
