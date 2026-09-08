using UnityEngine;

/// <summary>
/// 背包也是可存放的物品。继承的 Cells 表示外部占格，InteriorLayout 表示内部容量。
/// 每个背包实例的实际内容应由运行时系统保存，不写入共享配置资产。
/// </summary>
[CreateAssetMenu(menuName = "Inventory/Backpack Item Definition", fileName = "BackpackItemDefinition")]
public sealed class BackpackItemDefinition : InventoryItemDefinition
{
    [Tooltip("背包内部的存储区域。物品网格编辑的是背包自身放入其他容器时的占格形状。")]
    public InventoryLayoutDefinition InteriorLayout;
}
