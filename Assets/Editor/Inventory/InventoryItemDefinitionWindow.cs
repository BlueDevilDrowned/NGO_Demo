using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class InventoryItemDefinitionWindow : EditorWindow
{
    [SerializeField] private InventoryItemDefinition target;
    [SerializeField] private int selected;
    [SerializeField] private bool selectingInfo;
    [SerializeField] private bool infoCollapsed;
    private VisualElement detail;
    private VisualElement infoDetail;
    private VisualElement infoBody;
    [MenuItem("Tools/Inventory/Item Shape Editor")]
    public static void Open() => GetWindow<InventoryItemDefinitionWindow>("物品网格配置");
    public static void Open(InventoryItemDefinition item)
    {
        var window = GetWindow<InventoryItemDefinitionWindow>("物品网格配置");
        window.target = item; window.selected = 0; window.CreateGUI(); window.Focus();
    }
    private void OnEnable() => Undo.undoRedoPerformed += CreateGUI;
    private void OnDisable() => Undo.undoRedoPerformed -= CreateGUI;
    public void CreateGUI()
    {
        rootVisualElement.Unbind(); rootVisualElement.Clear();
        var toolbar = new Toolbar();
        toolbar.Add(new Label("物品配置") { style = { flexGrow = 1 } });
        toolbar.Add(new ToolbarButton(InventoryItemEditorSettingsWindow.Open) { text = "设置" });
        toolbar.Add(new ToolbarButton(() => { if (target != null) AssetDatabase.SaveAssetIfDirty(target); }) { text = "保存" });
        rootVisualElement.Add(toolbar);
        var item = new ObjectField("物品") { objectType = typeof(InventoryItemDefinition), allowSceneObjects = false, value = target };
        item.RegisterValueChangedCallback(e => { target = e.newValue as InventoryItemDefinition; selected = 0; selectingInfo = false; CreateGUI(); });
        rootVisualElement.Add(item);
        if (target == null) return;
        RegisterTarget();

        var split = new TwoPaneSplitView(0, 240, TwoPaneSplitViewOrientation.Horizontal);
        split.style.flexGrow = 1; split.style.minHeight = 300;
        var left = new ScrollView();
        var content = new TwoPaneSplitView(1, 300, TwoPaneSplitViewOrientation.Horizontal);
        detail = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        var infoSplit = new TwoPaneSplitView(0, 210, TwoPaneSplitViewOrientation.Horizontal);
        var infoList = new ScrollView();
        infoDetail = new VisualElement();
        infoBody = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        var collapse = new Button();
        collapse.clicked += () =>
        {
            infoCollapsed = !infoCollapsed;
            infoBody.style.display = infoCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
            collapse.text = infoCollapsed ? "展开信息面板" : "收起信息面板";
        };
        collapse.text = infoCollapsed ? "展开信息面板" : "收起信息面板";
        infoBody.style.display = infoCollapsed ? DisplayStyle.None : DisplayStyle.Flex;
        infoDetail.Add(collapse);
        infoDetail.Add(infoBody);
        infoSplit.Add(infoList);
        infoSplit.Add(infoDetail);
        detail.style.paddingLeft = detail.style.paddingRight = 12;
        infoList.style.paddingLeft = infoList.style.paddingRight = 8;
        content.Add(detail); content.Add(infoSplit);
        split.Add(left); split.Add(content); rootVisualElement.Add(split);
        var data = new SerializedObject(target);
        foreach (string name in new[] { "DisplayName", "Icon" })
        {
            var field = new PropertyField(data.FindProperty(name)); left.Add(field); field.Bind(data);
        }
        left.Add(new Label("功能模组"));
        for (int i = 0; i < target.Modules.Count; i++)
        {
            int index = i;
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var select = new Button(() => { selected = index; selectingInfo = false; ShowSelection(); }) { text = ModuleName(target.Modules[i]) };
            select.style.flexGrow = 1; row.Add(select);
            row.Add(new Button(() =>
            {
                var edit = new SerializedObject(target);
                edit.FindProperty("Modules").DeleteArrayElementAtIndex(index);
                edit.ApplyModifiedProperties();
                target.SynchronizeModuleInfos();
                EditorUtility.SetDirty(target);
                CreateGUI();
            }) { text = "删除" });
            left.Add(row);
        }
        left.Add(new Button(() => ItemModuleSearchWindow.Open(target, CreateGUI)) { text = "添加模组…" });
        BuildInfoList(infoList);
        ClampSelection();
        ShowSelection();
    }

    private void BuildInfoList(VisualElement parent)
    {
        parent.Add(new Label("模组信息") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
        parent.Add(new HelpBox("模组信息由左侧模组自动维护。这里只能选择并配置。", HelpBoxMessageType.Info));

        for (int i = 0; i < target.ModuleInfos.Count; i++)
        {
            int index = i;
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var select = new Button(() => { selected = index; selectingInfo = true; ShowSelection(); })
            {
                text = InfoName(target.ModuleInfos[i])
            };
            select.style.flexGrow = 1;
            row.Add(select);
            parent.Add(row);
        }
    }

    private static string ModuleName(ItemModule module) => module is StorageShapeModule ? "物品占格" : module is BackpackModule ? "背包" : module is WeaponModule ? "武器" : module is DropModule ? "掉落物" : module?.GetType().Name ?? "缺失模组";
    private static string InfoName(ItemModuleInfo info) => info is WeaponModuleInfo ? "武器信息" : info is DropModuleInfo ? "掉落物信息" : info?.GetType().Name ?? "缺失信息";

    private void ClampSelection()
    {
        int count = selectingInfo ? target.ModuleInfos.Count : target.Modules.Count;
        if (count == 0 && selectingInfo)
        {
            selectingInfo = false;
            count = target.Modules.Count;
        }
        selected = Mathf.Clamp(selected, 0, Mathf.Max(0, count - 1));
    }

    private void ShowSelection()
    {
        if (selectingInfo)
        {
            ShowInfoSelection();
            return;
        }

        detail.Unbind(); detail.Clear();
        if (target.Modules.Count == 0) { detail.Add(new Label("点击添加模组开始配置。")); return; }
        ItemModule module = target.Modules[selected];
        detail.Add(new Label(ModuleName(module)) { style = { unityFontStyleAndWeight = FontStyle.Bold } });
        if (module is StorageShapeModule shape)
        {
            var rotate = new Toggle("允许旋转") { value = shape.CanRotate };
            rotate.RegisterValueChangedCallback(e => Change(() => shape.CanRotate = e.newValue)); detail.Add(rotate);
            AddGrid(detail, () => shape.GridWidth, () => shape.GridHeight, () => shape.Cells,
                (w, h) => { shape.GridWidth = w; shape.GridHeight = h; });
        }
        else if (module is BackpackModule backpack)
        {
            for (int i = 0; i < backpack.Regions.Count; i++)
            {
                int index = i;
                InventoryRegionDefinition region = backpack.Regions[i];
                var group = new Foldout { text = "区域 " + i, value = true }; detail.Add(group);
                var name = new TextField("名称") { value = region.DisplayName };
                name.RegisterValueChangedCallback(e => Change(() => region.DisplayName = e.newValue)); group.Add(name);
                group.Add(new Button(() => { Change(() => backpack.Regions.RemoveAt(index)); ShowSelection(); }) { text = "删除区域" });
                AddGrid(group, () => region.Width, () => region.Height, () => region.EnabledCells,
                    (w, h) => { region.Width = w; region.Height = h; });
            }
            detail.Add(new Button(() => { Change(() => backpack.Regions.Add(new InventoryRegionDefinition())); ShowSelection(); }) { text = "添加区域" });
        }
        else
        {
            var data = new SerializedObject(target);
            var field = new PropertyField(data.FindProperty("Modules").GetArrayElementAtIndex(selected));
            detail.Add(field); field.Bind(data);
        }
    }
    private void Change(Action mutation)
    {
        Undo.RecordObject(target, "编辑物品模组"); mutation(); EditorUtility.SetDirty(target);
    }

    private void ShowInfoSelection()
    {
        infoBody.Unbind();
        infoBody.Clear();
        if (target.ModuleInfos.Count == 0)
        {
            infoBody.Add(new Label("当前物品没有需要额外配置的信息。"));
            return;
        }

        ItemModuleInfo info = target.ModuleInfos[selected];
        infoBody.Add(new Label(InfoName(info))
        {
            style = { unityFontStyleAndWeight = FontStyle.Bold }
        });
        var data = new SerializedObject(target);
        var field = new PropertyField(
            data.FindProperty("ModuleInfos").GetArrayElementAtIndex(selected));
        infoBody.Add(field);
        field.Bind(data);
    }

    private void RegisterTarget()
    {
        InventoryItemEditorSettings settings = InventoryItemEditorSettings.Load();
        if (settings.Registry == null || !string.IsNullOrEmpty(settings.Registry.GetId(target))) return;
        Undo.RecordObject(settings.Registry, "注册物品");
        settings.Registry.Register(target);
        EditorUtility.SetDirty(settings.Registry);
    }
    private void AddGrid(VisualElement parent, Func<int> width, Func<int> height,
        Func<List<Vector2Int>> cells, Action<int, int> resize)
    {
        var widthField = new IntegerField("宽度") { value = width(), isDelayed = true };
        var heightField = new IntegerField("高度") { value = height(), isDelayed = true };
        parent.Add(widthField); parent.Add(heightField);
        parent.Add(new HelpBox("左键：单格取反；右键拖动：矩形取反（松开提交）；Esc：取消。\n蓝色 ■ 为启用格子，灰色 · 为关闭格子，橙框为选区。原点在左下角。缩小前请先关闭范围外格子。", HelpBoxMessageType.Info));
        var grid = new InventoryGridEditorElement(width, height, () => new HashSet<Vector2Int>(cells()), (from, to) => Change(() =>
        {
            for (int y = Mathf.Min(from.y, to.y); y <= Mathf.Max(from.y, to.y); y++)
            for (int x = Mathf.Min(from.x, to.x); x <= Mathf.Max(from.x, to.x); x++)
            {
                var cell = new Vector2Int(x, y);
                if (cells().Contains(cell)) cells().RemoveAll(c => c == cell); else cells().Add(cell);
            }
        }));
        parent.Add(grid);
        Action resizeGrid = () =>
        {
            int w = Mathf.Clamp(widthField.value, 1, 128), h = Mathf.Clamp(heightField.value, 1, 128);
            foreach (Vector2Int cell in cells()) { w = Mathf.Max(w, cell.x + 1); h = Mathf.Max(h, cell.y + 1); }
            Change(() => resize(w, h));
            widthField.SetValueWithoutNotify(w); heightField.SetValueWithoutNotify(h); grid.Rebuild();
        };
        widthField.RegisterValueChangedCallback(e => resizeGrid());
        heightField.RegisterValueChangedCallback(e => resizeGrid());
    }
}
public static class InventoryItemDefinitionAssetOpener
{
    [UnityEditor.Callbacks.OnOpenAsset]
    private static bool OnOpenAsset(int instanceId, int line)
    {
        var item = EditorUtility.InstanceIDToObject(instanceId) as InventoryItemDefinition;
        if (item == null) return false;
        InventoryItemDefinitionWindow.Open(item); return true;
    }
}
