using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class InventoryItemEditorSettings : ScriptableObject
{
    public InventoryItemRegistry Registry;
    public List<string> ModuleSearchFolders = new List<string> { "Assets/Scripts/Inventory/Config/Modules" };
    public bool IncludeSubfolders = true;

    public static InventoryItemEditorSettings Load()
    {
        string[] guids = AssetDatabase.FindAssets("t:InventoryItemEditorSettings");
        if (guids.Length > 0) return AssetDatabase.LoadAssetAtPath<InventoryItemEditorSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        var settings = CreateInstance<InventoryItemEditorSettings>();
        string folder = "Assets/Editor/Inventory";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Editor", "Inventory");
        AssetDatabase.CreateAsset(settings, folder + "/InventoryItemEditorSettings.asset");
        AssetDatabase.SaveAssets();
        return settings;
    }
}

public sealed class InventoryItemEditorSettingsWindow : EditorWindow
{
    private InventoryItemEditorSettings settings;
    private SerializedObject serialized;

    public static void Open()
    {
        var window = GetWindow<InventoryItemEditorSettingsWindow>("物品编辑器设置");
        window.settings = InventoryItemEditorSettings.Load();
        window.serialized = new SerializedObject(window.settings);
        window.CreateGUI();
    }

    public void CreateGUI()
    {
        rootVisualElement.Clear();
        if (settings == null) settings = InventoryItemEditorSettings.Load();
        serialized = new SerializedObject(settings);
        var folders = new PropertyField(serialized.FindProperty("ModuleSearchFolders"), "模组搜索目录");
        rootVisualElement.Add(new PropertyField(serialized.FindProperty("Registry"), "物品注册表"));
        rootVisualElement.Add(folders);
        rootVisualElement.Add(new PropertyField(serialized.FindProperty("IncludeSubfolders"), "包含子目录"));
        rootVisualElement.Bind(serialized);
        rootVisualElement.Add(new HelpBox("添加模组时，只会显示这些目录中的 ItemModule 子类脚本。", HelpBoxMessageType.Info));
        rootVisualElement.Add(new Button(() => { serialized.ApplyModifiedProperties(); AssetDatabase.SaveAssets(); }) { text = "保存设置" });
    }
}

