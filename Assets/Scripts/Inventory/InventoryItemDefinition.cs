using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "ItemDefinition")]
public class InventoryItemDefinition : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;
    public bool CanRotate = true;
    [Min(1)] public int GridWidth = 3;
    [Min(1)] public int GridHeight = 3;
    public List<Vector2Int> Cells = new List<Vector2Int>();
    [SerializeField] public List<ItemModuleDefinition> Modules = new List<ItemModuleDefinition>();

    protected virtual void OnValidate()
    {
        if (Cells == null) Cells = new List<Vector2Int>();
        GridWidth = Mathf.Max(1, GridWidth);
        GridHeight = Mathf.Max(1, GridHeight);
        for (int i = Cells.Count - 1; i >= 0; i--)
            if (Cells[i].x < 0 || Cells[i].y < 0) Cells.RemoveAt(i);
    }
}
