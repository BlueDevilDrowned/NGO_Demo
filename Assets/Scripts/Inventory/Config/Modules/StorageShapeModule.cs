using System;
using System.Collections.Generic;
using UnityEngine;

public enum ItemQuality : byte
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic
}

[Serializable]
public struct ItemQualityColor
{
    public ItemQuality Quality;
    public Color Color;
}

[Serializable]
public sealed class StorageShapeModule : ItemModule
{
    [Header("Quality")]
    [Tooltip("品质顺序：白、绿、蓝、紫、金、红。当前仅保存配置，不参与实际逻辑。")]
    public ItemQuality Quality = ItemQuality.Common;
    public Color QualityColor => GetQualityColor(Quality);

    public static Color GetQualityColor(ItemQuality quality)
    {
        switch (quality)
        {
            case ItemQuality.Uncommon: return new Color(0.2f, 0.85f, 0.25f);
            case ItemQuality.Rare: return new Color(0.2f, 0.45f, 1f);
            case ItemQuality.Epic: return new Color(0.7f, 0.25f, 0.95f);
            case ItemQuality.Legendary: return new Color(1f, 0.65f, 0.05f);
            case ItemQuality.Mythic: return new Color(0.95f, 0.12f, 0.12f);
            default: return Color.white;
        }
    }

    [Min(1)] public int GridWidth = 3;
    [Min(1)] public int GridHeight = 3;
    public bool CanRotate = true;
    public List<Vector2Int> Cells = new List<Vector2Int>();

    public override void CollectInteractionOptions(InventoryItemDefinition item,List<ItemInteractionOption> options)
        => options.Add(new ItemInteractionOption("pickup","拾取 "+ItemName(item),true,this,item.Icon));
    public override bool CanInteract(InventoryItemDefinition item,Actor actor) => actor?.inventorySystem!=null;
    public override bool OnInteract(ItemInstance item,Actor actor,string optionId)
        => optionId=="pickup" && actor.IsServer && actor.inventorySystem.TryAutoPlace(item,out _);
}
