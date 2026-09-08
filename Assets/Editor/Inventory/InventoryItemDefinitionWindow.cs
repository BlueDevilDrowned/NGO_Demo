using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>物品网格配置窗口；右键拖动只预览，释放后作为一次可撤销操作提交。</summary>
public sealed class InventoryItemDefinitionWindow : EditorWindow
{
    private const int CellSize = 32;
    [SerializeField] private InventoryItemDefinition target;
    private VisualElement grid;
    private readonly List<Label> cellViews = new List<Label>();
    private int width, height;
    private int dragPointer = -1;
    private Vector2Int dragStart, dragEnd;

    [MenuItem("Tools/Inventory/Item Shape Editor")]
    public static void Open() => GetWindow<InventoryItemDefinitionWindow>("物品网格配置");

    public static void Open(InventoryItemDefinition item)
    {
        var window = GetWindow<InventoryItemDefinitionWindow>("物品网格配置");
        window.target = item;
        window.CreateGUI();
        window.Focus();
    }

    private void OnEnable() => Undo.undoRedoPerformed += Refresh;
    private void OnDisable()
    {
        Undo.undoRedoPerformed -= Refresh;
        CancelDrag();
    }
    private void Refresh() => CreateGUI();

    public void CreateGUI()
    {
        CancelDrag();
        rootVisualElement.Unbind();
        rootVisualElement.Clear();
        cellViews.Clear();
        grid = null;
        var root = rootVisualElement;
        root.style.paddingLeft = root.style.paddingRight = 10;
        root.style.paddingTop = root.style.paddingBottom = 10;

        var itemField = new ObjectField("物品配置")
        {
            objectType = typeof(InventoryItemDefinition), allowSceneObjects = false, value = target
        };
        itemField.RegisterValueChangedCallback(e =>
        {
            target = e.newValue as InventoryItemDefinition;
            CreateGUI();
        });
        root.Add(itemField);
        if (target == null)
        {
            root.Add(new HelpBox("请选择物品配置，或在 Project 中右键 Create > Inventory > Item Definition 创建。", HelpBoxMessageType.Info));
            return;
        }

        // 使用 SerializedObject 绑定，元数据修改支持撤销和 Unity 的资产脏标记。
        var serialized = new SerializedObject(target);
        foreach (string property in new[] { "DisplayName", "Icon", "CanRotate" })
        {
            var field = new PropertyField(serialized.FindProperty(property));
            root.Add(field);
            field.Bind(serialized);
        }
        BuildModulesPanel(root);
        if (target is BackpackItemDefinition)
        {
            var interior = new PropertyField(serialized.FindProperty(nameof(BackpackItemDefinition.InteriorLayout)), "背包内部布局");
            root.Add(interior);
            interior.Bind(serialized);
            root.Add(new HelpBox("下方网格表示背包作为物品放入其他背包时的占格形状；内部容量由上方布局资产定义。", HelpBoxMessageType.Info));
        }
        width = Mathf.Max(1, target.GridWidth);
        height = Mathf.Max(1, target.GridHeight);
        // 兼容旧资产：打开时显示所有已有格子，不因默认宽高隐藏形状。
        foreach (Vector2Int cell in target.Cells)
        {
            width = Mathf.Max(width, cell.x + 1);
            height = Mathf.Max(height, cell.y + 1);
        }
        var widthField = new IntegerField("网格宽度") { value = width, isDelayed = true };
        var heightField = new IntegerField("网格高度") { value = height, isDelayed = true };
        widthField.RegisterValueChangedCallback(e => Resize(e.newValue, height));
        heightField.RegisterValueChangedCallback(e => Resize(width, e.newValue));
        root.Add(widthField);
        root.Add(heightField);
        root.Add(new HelpBox("左键点击：单格取反。右键拖动：框选矩形，释放后范围取反。Esc：取消框选。\n■ 蓝色：物品包含该格子；· 灰色：不包含；橙色边框：待取反范围。左下角为 (0, 0)。", HelpBoxMessageType.Info));

        var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
        scroll.style.flexGrow = 1;
        root.Add(scroll);
        grid = new VisualElement { focusable = true };
        grid.style.width = width * CellSize;
        grid.style.height = height * CellSize;
        grid.style.flexShrink = 0;
        scroll.Add(grid);
        for (int row = 0; row < height; row++)
        for (int x = 0; x < width; x++)
        {
            var cell = new Label { pickingMode = PickingMode.Ignore };
            cell.style.position = Position.Absolute;
            cell.style.left = x * CellSize;
            cell.style.top = row * CellSize;
            cell.style.width = cell.style.height = CellSize;
            cell.style.unityTextAlign = TextAnchor.MiddleCenter;
            cell.style.borderLeftWidth = cell.style.borderRightWidth = 1;
            cell.style.borderTopWidth = cell.style.borderBottomWidth = 1;
            grid.Add(cell);
            cellViews.Add(cell);
        }
        grid.RegisterCallback<PointerDownEvent>(OnPointerDown);
        grid.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        grid.RegisterCallback<PointerUpEvent>(OnPointerUp);
        grid.RegisterCallback<PointerCaptureOutEvent>(e => { dragPointer = -1; Paint(); });
        grid.RegisterCallback<PointerCancelEvent>(e => CancelDrag());
        grid.RegisterCallback<KeyDownEvent>(e =>
        {
            if (e.keyCode != KeyCode.Escape) return;
            CancelDrag();
            e.StopPropagation();
        });
        root.Add(new Button(() => AssetDatabase.SaveAssetIfDirty(target)) { text = "保存配置" });
        Paint();
    }

    private void BuildModulesPanel(VisualElement root)
    {
        var box = new VisualElement();
        box.style.marginTop = 6;
        box.Add(new Label("功能模组") { style = { unityFontStyleAndWeight = FontStyle.Bold } });
        if (target.Modules == null) target.Modules = new List<ItemModuleDefinition>();
        for (int i = 0; i < target.Modules.Count; i++)
        {
            int index = i;
            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var field = new ObjectField { objectType = typeof(ItemModuleDefinition), value = target.Modules[i] };
            field.style.flexGrow = 1;
            field.SetEnabled(false);
            row.Add(field);
            var remove = new Button(() =>
            {
                Undo.RecordObject(target, "移除物品模组");
                target.Modules.RemoveAt(index);
                EditorUtility.SetDirty(target);
                CreateGUI();
            }) { text = "移除" };
            row.Add(remove);
            box.Add(row);
        }
        box.Add(new Button(ShowModuleMenu) { text = "添加模组" });
        root.Add(box);
    }

    private void ShowModuleMenu()
    {
        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("背包模组"), false, () => AddModule(typeof(BackpackModuleDefinition), "BackpackModule"));
        menu.ShowAsContext();
    }

    private void AddModule(System.Type type, string name)
    {
        string path = AssetDatabase.GetAssetPath(target);
        if (string.IsNullOrEmpty(path))
        {
            ShowNotification(new GUIContent("请先保存物品配置资产。"));
            return;
        }
        Undo.RecordObject(target, "添加物品模组");
        var module = ScriptableObject.CreateInstance(type) as ItemModuleDefinition;
        module.name = name;
        AssetDatabase.AddObjectToAsset(module, path);
        AssetDatabase.SaveAssets();
        target.Modules.Add(module);
        EditorUtility.SetDirty(target);
        CreateGUI();
    }

    private void Resize(int newWidth, int newHeight)
    {
        newWidth = Mathf.Clamp(newWidth, 1, 128);
        newHeight = Mathf.Clamp(newHeight, 1, 128);
        int requestedWidth = newWidth, requestedHeight = newHeight;
        // 缩小画布不能悄悄删除或隐藏物品格子。
        foreach (Vector2Int cell in target.Cells)
        {
            newWidth = Mathf.Max(newWidth, cell.x + 1);
            newHeight = Mathf.Max(newHeight, cell.y + 1);
        }
        Undo.RecordObject(target, "调整物品网格尺寸");
        target.GridWidth = newWidth;
        target.GridHeight = newHeight;
        EditorUtility.SetDirty(target);
        CreateGUI();
        if (newWidth != requestedWidth || newHeight != requestedHeight)
            ShowNotification(new GUIContent("缩小范围前，请先关闭范围外的格子。"));
    }

    private Vector2Int CellAt(Vector3 position)
    {
        Vector2 local = grid.WorldToLocal(new Vector2(position.x, position.y));
        return new Vector2Int(Mathf.Clamp(Mathf.FloorToInt(local.x / CellSize), 0, width - 1),
            height - 1 - Mathf.Clamp(Mathf.FloorToInt(local.y / CellSize), 0, height - 1));
    }

    private void OnPointerDown(PointerDownEvent e)
    {
        if (target == null || dragPointer >= 0 || (e.button != 0 && e.button != 1)) return;
        grid.Focus();
        var cell = CellAt(e.position);
        if (e.button == 0) Invert(cell, cell);
        else
        {
            dragStart = dragEnd = cell;
            dragPointer = e.pointerId;
            grid.CapturePointer(dragPointer);
            Paint();
        }
        e.StopPropagation();
    }

    private void OnPointerMove(PointerMoveEvent e)
    {
        if (e.pointerId != dragPointer) return;
        dragEnd = CellAt(e.position);
        Paint();
        e.StopPropagation();
    }

    private void OnPointerUp(PointerUpEvent e)
    {
        if (e.button != 1 || e.pointerId != dragPointer) return;
        dragEnd = CellAt(e.position);
        var start = dragStart;
        var end = dragEnd;
        CancelDrag();
        Invert(start, end);
        e.StopPropagation();
    }

    private void CancelDrag()
    {
        int pointer = dragPointer;
        dragPointer = -1;
        if (grid != null && pointer >= 0 && grid.HasPointerCapture(pointer)) grid.ReleasePointer(pointer);
        Paint();
    }

    private void Invert(Vector2Int start, Vector2Int end)
    {
        Undo.RecordObject(target, "物品网格范围取反");
        for (int y = Mathf.Min(start.y, end.y); y <= Mathf.Max(start.y, end.y); y++)
        for (int x = Mathf.Min(start.x, end.x); x <= Mathf.Max(start.x, end.x); x++)
        {
            var cell = new Vector2Int(x, y);
            if (target.Cells.Contains(cell)) target.Cells.RemoveAll(c => c == cell);
            else target.Cells.Add(cell);
        }
        target.GridWidth = width;
        target.GridHeight = height;
        EditorUtility.SetDirty(target);
        Paint();
    }

    private void Paint()
    {
        if (grid == null || target == null) return;
        var enabled = new HashSet<Vector2Int>(target.Cells);
        for (int i = 0; i < cellViews.Count; i++)
        {
            int x = i % width, y = height - 1 - i / width;
            bool active = enabled.Contains(new Vector2Int(x, y));
            bool selected = dragPointer >= 0 && x >= Mathf.Min(dragStart.x, dragEnd.x) &&
                x <= Mathf.Max(dragStart.x, dragEnd.x) && y >= Mathf.Min(dragStart.y, dragEnd.y) && y <= Mathf.Max(dragStart.y, dragEnd.y);
            var view = cellViews[i];
            view.text = active ? "■" : "·";
            view.style.color = Color.white;
            view.style.backgroundColor = active ? new Color(0.15f, 0.45f, 0.7f) : new Color(0.2f, 0.2f, 0.2f);
            Color border = selected ? new Color(1f, 0.65f, 0.1f) : new Color(0.4f, 0.4f, 0.4f);
            view.style.borderLeftColor = view.style.borderRightColor = border;
            view.style.borderTopColor = view.style.borderBottomColor = border;
        }
    }
}

public static class InventoryItemDefinitionAssetOpener
{
    [UnityEditor.Callbacks.OnOpenAsset]
    private static bool OnOpenAsset(int instanceId, int line)
    {
        var item = EditorUtility.InstanceIDToObject(instanceId) as InventoryItemDefinition;
        if (item == null) return false;
        InventoryItemDefinitionWindow.Open(item);
        return true;
    }
}
