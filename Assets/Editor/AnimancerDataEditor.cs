using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Animancer;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AnimancerData))]
public sealed class AnimancerDataEditor : Editor
{
    private bool showPrewarmEntries=true;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        if(GUILayout.Button("生成动画初始化列表"))
            GeneratePrewarmList();

        DrawPrewarmEntries((AnimancerData)target);
    }

    private void GeneratePrewarmList()
    {
        AnimancerData data=(AnimancerData)target;
        List<AnimationPrewarmEntry> entries=
            AnimancerDataPrewarmScanner.Scan(data);

        Undo.RecordObject(data,"Generate Animancer Prewarm List");
        data.ReplacePrewarmEntries(entries);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        serializedObject.Update();

        Debug.Log(
            $"Generated {entries.Count} Animancer prewarm entries for {data.name}.",
            data);
    }

    private void DrawPrewarmEntries(AnimancerData data)
    {
        IReadOnlyList<AnimationPrewarmEntry> entries=data.PrewarmEntries;
        showPrewarmEntries=EditorGUILayout.Foldout(
            showPrewarmEntries,
            $"已生成的动画初始化列表 ({entries.Count})",
            true);
        if(!showPrewarmEntries)return;

        using(new EditorGUI.IndentLevelScope())
        using(new EditorGUI.DisabledScope(true))
        {
            for(int i=0;i<entries.Count;i++)
            {
                AnimationPrewarmEntry entry=entries[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(
                    entry.Transition,
                    typeof(TransitionAsset),
                    false);
                EditorGUILayout.IntField("Layer",entry.Layer,GUILayout.Width(110f));
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}

internal static class AnimancerDataPrewarmScanner
{
    private const BindingFlags FieldFlags=
        BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

    public static List<AnimationPrewarmEntry> Scan(AnimancerData data)
    {
        var entries=new List<AnimationPrewarmEntry>();
        var keys=new HashSet<PrewarmKey>();
        var visited=new HashSet<object>(ReferenceComparer.Instance);

        CollectFields(data,0,entries,keys,visited);
        entries.Sort(CompareEntries);
        return entries;
    }

    private static void Collect(
        object value,
        int inheritedLayer,
        List<AnimationPrewarmEntry> entries,
        HashSet<PrewarmKey> keys,
        HashSet<object> visited)
    {
        if(value==null)return;

        if(value is TransitionAsset transition)
        {
            var key=new PrewarmKey(transition,inheritedLayer);
            if(keys.Add(key))
                entries.Add(new AnimationPrewarmEntry(transition,inheritedLayer));
            return;
        }

        Type type=value.GetType();
        if(type.IsPrimitive||type.IsEnum||value is string||value is UnityEngine.Object)
            return;

        if(value is IEnumerable enumerable)
        {
            foreach(object item in enumerable)
                Collect(item,inheritedLayer,entries,keys,visited);
            return;
        }

        if(!type.IsValueType&&!visited.Add(value))return;
        CollectFields(value,inheritedLayer,entries,keys,visited);
    }

    private static void CollectFields(
        object container,
        int inheritedLayer,
        List<AnimationPrewarmEntry> entries,
        HashSet<PrewarmKey> keys,
        HashSet<object> visited)
    {
        FieldInfo[] fields=container.GetType().GetFields(FieldFlags);
        for(int i=0;i<fields.Length;i++)
        {
            FieldInfo field=fields[i];
            if(!IsSerializedField(field)||
               field.IsDefined(typeof(IgnoreAnimationPrewarmAttribute),true))
                continue;

            AnimationLayerAttribute layerAttribute=
                field.GetCustomAttribute<AnimationLayerAttribute>(true);
            int layer=layerAttribute?.Layer??inheritedLayer;
            if(layer<0)
            {
                Debug.LogError(
                    $"{container.GetType().Name}.{field.Name} has a negative animation layer.");
                continue;
            }

            Collect(field.GetValue(container),layer,entries,keys,visited);
        }
    }

    private static bool IsSerializedField(FieldInfo field)
    {
        if(field.IsStatic||field.IsNotSerialized)return false;
        return field.IsPublic||field.IsDefined(typeof(SerializeField),true);
    }

    private static int CompareEntries(
        AnimationPrewarmEntry left,
        AnimationPrewarmEntry right)
    {
        int layerComparison=left.Layer.CompareTo(right.Layer);
        if(layerComparison!=0)return layerComparison;

        string leftPath=AssetDatabase.GetAssetPath(left.Transition);
        string rightPath=AssetDatabase.GetAssetPath(right.Transition);
        return string.Compare(leftPath,rightPath,StringComparison.Ordinal);
    }

    private readonly struct PrewarmKey : IEquatable<PrewarmKey>
    {
        private readonly TransitionAsset transition;
        private readonly int layer;

        public PrewarmKey(TransitionAsset transition,int layer)
        {
            this.transition=transition;
            this.layer=layer;
        }

        public bool Equals(PrewarmKey other)
        {
            return ReferenceEquals(transition,other.transition)&&layer==other.layer;
        }

        public override bool Equals(object obj)
        {
            return obj is PrewarmKey other&&Equals(other);
        }

        public override int GetHashCode()
        {
            return (RuntimeHelpers.GetHashCode(transition)*397)^layer;
        }
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance=new();

        public new bool Equals(object x,object y)
        {
            return ReferenceEquals(x,y);
        }

        public int GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
