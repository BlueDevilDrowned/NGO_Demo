using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[CustomEditor(typeof(ActorBrainSo))]
public sealed class ActorBrainSoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ActorBrainSo brain=(ActorBrainSo)target;

        EditorGUILayout.HelpBox(
            "状态图资源请在专用窗口中配置。双击资源也会打开该窗口。",
            MessageType.Info);

        using(new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField(
                "Asset",
                brain,
                typeof(ActorBrainSo),
                false);
        }

        if(GUILayout.Button("打开状态配置窗口",GUILayout.Height(28)))
            ActorBrainStateGraphWindow.Open(brain);

        EditorGUILayout.Space(4f);
        DrawSummary(brain);
    }

    private static void DrawSummary(ActorBrainSo brain)
    {
        if(brain==null)return;

        int fullBodyCount=brain.FullBody?.AvailableStates?.Count??0;
        int firstPersonCount=brain.FirstPerson?.AvailableStates?.Count??0;
        int firstPersonTransitionCount=
            brain.FirstPerson?.Transitions?.Count??0;

        EditorGUILayout.LabelField("Full Body States",fullBodyCount.ToString());
        EditorGUILayout.LabelField(
            "First Person States",
            firstPersonCount.ToString());
        EditorGUILayout.LabelField(
            "First Person Relations",
            firstPersonTransitionCount.ToString());
    }
}

public sealed class ActorBrainStateGraphWindow : EditorWindow
{
    private enum Section
    {
        Overview,
        Shared,
        FullBody,
        FirstPerson,
    }

    private static readonly GUIContent[]sectionLabels=
    {
        new("Overview"),
        new("Shared"),
        new("Full Body"),
        new("First Person"),
    };

    private ActorBrainSo brain;
    private SerializedObject serializedBrain;
    private Section section;

    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceId,int line)
    {
        ActorBrainSo asset=EditorUtility.EntityIdToObject(instanceId)
            as ActorBrainSo;
        if(asset==null)return false;

        Open(asset);
        return true;
    }

    [MenuItem("Assets/Actor Brain/Open State Graph")]
    private static void OpenSelectedAsset()
    {
        if(Selection.activeObject is ActorBrainSo asset)
            Open(asset);
    }

    [MenuItem("Assets/Actor Brain/Open State Graph",true)]
    private static bool ValidateOpenSelectedAsset()
    {
        return Selection.activeObject is ActorBrainSo;
    }

    public static void Open(ActorBrainSo asset)
    {
        if(asset==null)return;

        ActorBrainStateGraphWindow window=GetWindow<
            ActorBrainStateGraphWindow>("Actor Brain");
        window.SetAsset(asset);
        window.Show();
    }

    private void OnEnable()
    {
        minSize=new Vector2(760f,480f);
        if(brain!=null)
            serializedBrain=new SerializedObject(brain);
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void SetAsset(ActorBrainSo asset)
    {
        brain=asset;
        serializedBrain=new SerializedObject(brain);
        titleContent=new GUIContent($"Actor Brain - {brain.name}");
        Repaint();
    }

    private void OnGUI()
    {
        if(brain==null)
        {
            EditorGUILayout.HelpBox(
                "没有选择 ActorBrainSo 资源。",
                MessageType.Info);
            return;
        }

        serializedBrain.Update();
        DrawToolbar();
        EditorGUILayout.Space(6f);

        switch(section)
        {
            case Section.Overview:
                DrawOverview();
                break;
            case Section.Shared:
                DrawShared();
                break;
            case Section.FullBody:
                DrawFullBody();
                break;
            case Section.FirstPerson:
                DrawFirstPerson();
                break;
        }

        if(serializedBrain.ApplyModifiedProperties())
            EditorUtility.SetDirty(brain);
    }

    private void DrawToolbar()
    {
        using(new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            EditorGUILayout.LabelField(
                brain.name,
                EditorStyles.toolbarButton,
                GUILayout.Width(220f));

            section=(Section)GUILayout.Toolbar(
                (int)section,
                sectionLabels,
                EditorStyles.toolbarButton,
                GUILayout.ExpandWidth(true));

            if(GUILayout.Button(
                   "Ping",
                   EditorStyles.toolbarButton,
                   GUILayout.Width(55f)))
                EditorGUIUtility.PingObject(brain);
        }
    }

    private void DrawOverview()
    {
        EditorGUILayout.LabelField("Actor Brain State Graph",EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);

        SerializedProperty initialPerspective=serializedBrain.FindProperty(
            "InitialPerspectiveMode");
        EditorGUILayout.PropertyField(initialPerspective);

        DrawCount("Shared States",serializedBrain.FindProperty("SharedStates"));
        DrawCount("Full Body States",serializedBrain.FindProperty("FullBody"),"AvailableStates");
        DrawCount("First Person States",serializedBrain.FindProperty("FirstPerson"),"AvailableStates");
        DrawCount("First Person Relations",serializedBrain.FindProperty("FirstPerson"),"Transitions");

        EditorGUILayout.Space(8f);
        EditorGUILayout.HelpBox(
            "关系配置只描述允许的状态边。具体进入条件由状态类的 CanEnterFrom() 决定。",
            MessageType.None);
    }

    private void DrawShared()
    {
        EditorGUILayout.LabelField("Shared",EditorStyles.boldLabel);
        DrawList(serializedBrain.FindProperty("SharedStates"),"Shared States");
        DrawList(
            serializedBrain.FindProperty("SharedTransitions"),
            "Shared Transitions");
    }

    private void DrawFullBody()
    {
        SerializedProperty graph=serializedBrain.FindProperty("FullBody");
        if(graph==null)
        {
            EditorGUILayout.HelpBox("FullBody 配置不存在。",MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Full Body",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(graph.FindPropertyRelative("InitialState"));
        DrawList(
            graph.FindPropertyRelative("AvailableStates"),
            "Available States");
        DrawList(
            graph.FindPropertyRelative("GlobalTransitions"),
            "Global Transitions");
    }

    private void DrawFirstPerson()
    {
        SerializedProperty graph=serializedBrain.FindProperty("FirstPerson");
        if(graph==null)
        {
            EditorGUILayout.HelpBox(
                "FirstPerson 配置不存在。",
                MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("First Person",EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(graph.FindPropertyRelative("InitialState"));
        DrawList(
            graph.FindPropertyRelative("AvailableStates"),
            "Available States");
        DrawFirstPersonTransitions(
            graph.FindPropertyRelative("Transitions"));
    }

    private static void DrawCount(
        string label,
        SerializedProperty property,
        string childName=null)
    {
        if(property==null)return;
        SerializedProperty value=string.IsNullOrEmpty(childName)
            ?property
            :property.FindPropertyRelative(childName);
        if(value==null)return;

        EditorGUILayout.LabelField(label,value.arraySize.ToString());
    }

    private static void DrawList(SerializedProperty list,string label)
    {
        if(list==null)return;

        EditorGUILayout.Space(4f);
        EditorGUILayout.PropertyField(
            list,
            new GUIContent(label),
            true);
    }

    private static void DrawFirstPersonTransitions(SerializedProperty list)
    {
        if(list==null)return;

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Presentation Relations",EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "这里配置允许的来源/目标关系；进入条件由目标状态的 CanEnterFrom() 决定。",
            MessageType.None);

        for(int i=0;i<list.arraySize;i++)
        {
            SerializedProperty element=list.GetArrayElementAtIndex(i);
            SerializedProperty target=element.FindPropertyRelative("TargetState");
            SerializedProperty priority=element.FindPropertyRelative("Priority");
            SerializedProperty sources=element.FindPropertyRelative(
                "AllowedFromStates");

            using(new EditorGUILayout.VerticalScope("box"))
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(
                        target,
                        new GUIContent("Target"));
                    EditorGUILayout.PropertyField(
                        priority,
                        new GUIContent("Priority"),
                        GUILayout.Width(180f));

                    if(GUILayout.Button("X",GUILayout.Width(24f)))
                    {
                        list.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                EditorGUILayout.PropertyField(
                    sources,
                    new GUIContent("Allowed From"),
                    true);
            }
        }

        if(GUILayout.Button("添加第一人称关系"))
        {
            int index=list.arraySize;
            list.arraySize++;
            SerializedProperty element=list.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("TargetState").enumValueIndex=0;
            element.FindPropertyRelative("Priority").intValue=0;
            element.FindPropertyRelative("AllowedFromStates").arraySize=0;
        }
    }
}
