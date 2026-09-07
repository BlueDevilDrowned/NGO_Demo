using System;
using System.Collections.Generic;
namespace InventorySolver
{
    public static class  ShapeNormalizer
    {
        public static List<Cell>Normalize(IReadOnlyList<Cell>cells)
        {
            int minX=int.MaxValue;
            int minY=int.MaxValue;
            foreach(Cell cell in cells)
            {
                if(cell.X<minX)minX=cell.X;
                if(cell.Y<minY)minY=cell.Y;
            }

            var result =new List<Cell>(cells.Count);

            foreach(Cell cell in cells)
            {
                result.Add(new Cell(
                    cell.X-minX,
                    cell.Y-minY
                ));
            }
            return result;
        }
    }
}