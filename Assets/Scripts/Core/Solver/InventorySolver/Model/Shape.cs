using System.Collections.Generic;
namespace InventorySolver
{
public sealed class Shape
{
    public IReadOnlyList<Cell>Cells{get;}

    public Shape(IReadOnlyList<Cell> cells)
    {
        if(cells==null||cells.Count==0)
            throw new System.ArgumentException("Shape must contain cells.",nameof(cells));
        Cells=cells;
    }
}
}
