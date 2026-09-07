using System.Collections.Generic;
namespace InventorySolver
{
public sealed class ShapeVariant
{
    public ERotation Rotation{get;}
    public int Width{get;}
    public int Height{get;}
    public IReadOnlyList<Cell>Cells{get;}

    public ShapeVariant(ERotation rotation,int width,int height,IReadOnlyList<Cell>cells)
    {
        Rotation=rotation;
        Width=width;
        Height=height;
        Cells=cells;
    }
}
}
