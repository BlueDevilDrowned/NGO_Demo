using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ActorStateConfig))]
public sealed class ActorStateConfigDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return StateClassDropdown.GetHeight(3);
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position,label,property);
        StateClassDropdown.DrawLabel(ref position,label);
        StateClassDropdown.DrawProperty(
            ref position,
            property.FindPropertyRelative("StateType"),
            "State Type");
        StateClassDropdown.DrawType(
            ref position,
            property.FindPropertyRelative("stateClassName"),
            typeof(ActorBaseState));
        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(UpperBodyStateConfig))]
public sealed class UpperBodyStateConfigDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return StateClassDropdown.GetHeight(3);
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position,label,property);
        StateClassDropdown.DrawLabel(ref position,label);
        StateClassDropdown.DrawProperty(
            ref position,
            property.FindPropertyRelative("StateType"),
            "State Type");
        StateClassDropdown.DrawType(
            ref position,
            property.FindPropertyRelative("stateClassName"),
            typeof(UpperBodyState));
        EditorGUI.EndProperty();
    }
}

[CustomPropertyDrawer(typeof(FirstPersonStateConfig))]
public sealed class FirstPersonStateConfigDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(
        SerializedProperty property,
        GUIContent label)
    {
        return StateClassDropdown.GetHeight(3);
    }

    public override void OnGUI(
        Rect position,
        SerializedProperty property,
        GUIContent label)
    {
        EditorGUI.BeginProperty(position,label,property);
        StateClassDropdown.DrawLabel(ref position,label);
        StateClassDropdown.DrawProperty(
            ref position,
            property.FindPropertyRelative("StateType"),
            "State Type");
        StateClassDropdown.DrawType(
            ref position,
            property.FindPropertyRelative("stateClassName"),
            typeof(FirstPersonActorState));
        EditorGUI.EndProperty();
    }
}

internal static class StateClassDropdown
{
    private const float Spacing=2f;
    private static readonly Dictionary<Type,Type[]> TypeCacheByBase=new();

    public static float GetHeight(int lineCount)
    {
        return lineCount*EditorGUIUtility.singleLineHeight+
               (lineCount-1)*Spacing;
    }

    public static void DrawLabel(ref Rect position,GUIContent label)
    {
        Rect line=TakeLine(ref position);
        EditorGUI.LabelField(line,label,EditorStyles.boldLabel);
    }

    public static void DrawProperty(
        ref Rect position,
        SerializedProperty property,
        string label)
    {
        EditorGUI.PropertyField(
            TakeLine(ref position),
            property,
            new GUIContent(label));
    }

    public static void DrawType(
        ref Rect position,
        SerializedProperty classNameProperty,
        Type baseType)
    {
        Type[] types=GetValidTypes(baseType);
        string savedName=classNameProperty.stringValue;
        Type savedType=string.IsNullOrWhiteSpace(savedName)
            ?null
            :Type.GetType(savedName);

        List<string> names=new(types.Length+2){"<Not Selected>"};
        names.AddRange(types.Select(type=>type.FullName??type.Name));

        int selectedIndex=Array.IndexOf(types,savedType)+1;
        bool isInvalid=!string.IsNullOrWhiteSpace(savedName)&&selectedIndex==0;
        if(isInvalid)
        {
            selectedIndex=names.Count;
            names.Add($"<Invalid> {savedName}");
        }

        int newIndex=EditorGUI.Popup(
            TakeLine(ref position),
            "State Class",
            selectedIndex,
            names.ToArray());
        if(newIndex==selectedIndex)return;

        classNameProperty.stringValue=newIndex<=0||newIndex>types.Length
            ?string.Empty
            :types[newIndex-1].AssemblyQualifiedName;
    }

    private static Type[] GetValidTypes(Type baseType)
    {
        if(TypeCacheByBase.TryGetValue(baseType,out Type[] types))return types;

        types=TypeCache.GetTypesDerivedFrom(baseType)
            .Where(type=>
                type.IsClass&&
                !type.IsAbstract&&
                !type.IsGenericTypeDefinition&&
                type.GetConstructor(new[]{typeof(Actor)})!=null)
            .OrderBy(type=>type.FullName)
            .ToArray();
        TypeCacheByBase.Add(baseType,types);
        return types;
    }

    private static Rect TakeLine(ref Rect position)
    {
        Rect line=new(
            position.x,
            position.y,
            position.width,
            EditorGUIUtility.singleLineHeight);
        position.y+=EditorGUIUtility.singleLineHeight+Spacing;
        return line;
    }
}
