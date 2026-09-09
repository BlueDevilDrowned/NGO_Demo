using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>只在项目设置指定目录中发现可序列化的普通模组类型。</summary>
public sealed class ItemModuleSearchWindow : EditorWindow
{
    private InventoryItemDefinition item;
    private Action refresh;
    private readonly List<Type> types = new List<Type>();

    public static void Open(InventoryItemDefinition item, Action refresh)
    {
        var window = CreateInstance<ItemModuleSearchWindow>();
        window.item = item;
        window.refresh = refresh;
        window.titleContent = new GUIContent("添加模组");
        window.minSize = new Vector2(320, 240);
        window.ShowUtility();
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        types.Clear();
        var settings = InventoryItemEditorSettings.Load();
        var seen = new HashSet<Type>();
        foreach (string guid in AssetDatabase.FindAssets("t:MonoScript"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            bool included = false;
            foreach (string folder in settings.ModuleSearchFolders)
            {
                if (string.IsNullOrWhiteSpace(folder)) continue;
                string prefix = folder.Trim().Replace('\\', '/').TrimEnd('/') + "/";
                if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    (settings.IncludeSubfolders || path.Substring(prefix.Length).IndexOf('/') < 0))
                    included = true;
            }
            if (!included) continue;
            Type type = AssetDatabase.LoadAssetAtPath<MonoScript>(path)?.GetClass();
            if (type == null || type.IsAbstract || type.ContainsGenericParameters ||
                !typeof(ItemModule).IsAssignableFrom(type) ||
                !Attribute.IsDefined(type, typeof(SerializableAttribute), false) ||
                type.GetConstructor(Type.EmptyTypes) == null || !seen.Add(type)) continue;
            types.Add(type);
        }
        types.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
        var search = new ToolbarSearchField();
        rootVisualElement.Add(search);
        var results = new ScrollView();
        results.style.flexGrow = 1;
        rootVisualElement.Add(results);
        Action<string> show = query =>
        {
            results.Clear();
            foreach (Type type in types)
            {
                string label = type == typeof(BackpackModule) ? "背包模组 (BackpackModule)" : type.FullName;
                if (label.IndexOf(query ?? "", StringComparison.OrdinalIgnoreCase) < 0) continue;
                var button = new Button(() => Add(type)) { text = label };
                button.SetEnabled(item != null && !item.Modules.Exists(m => m?.GetType() == type));
                results.Add(button);
            }
            if (results.childCount == 0)
                results.Add(new Label("没有匹配模组，请检查搜索词或设置目录。"));
        };
        search.RegisterValueChangedCallback(e => show(e.newValue));
        show("");
        search.Focus();
    }

    private void Add(Type type)
    {
        if (item == null) return;
        var data = new SerializedObject(item);
        var modules = data.FindProperty("Modules");
        int index = modules.arraySize;
        modules.arraySize++;
        modules.GetArrayElementAtIndex(index).managedReferenceValue = Activator.CreateInstance(type);
        data.ApplyModifiedProperties();
        refresh?.Invoke();
        Close();
    }
}
