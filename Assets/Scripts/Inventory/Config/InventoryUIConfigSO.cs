using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/UI Config", fileName = "InventoryUIConfig")]
public sealed class InventoryUIConfigSO : ScriptableObject
{
    [Min(0f)] public float cellSpacing = 1f;
    public Color normalCellColor = new Color(1f, 1f, 1f, .12f);
    public Color disabledCellColor = new Color(1f, 1f, 1f, .025f);
    public Color allowedCellColor = new Color(.2f, 1f, .2f, .35f);
    public Color warningCellColor = new Color(1f, .1f, .1f, .5f);
    public Color originalPlacementColor = new Color(1f, 1f, 1f, .18f);
    [Range(0f, 1f)] public float itemOpacity = 1f;
    public bool preserveItemAspect = true;
}
