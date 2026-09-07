using System;
using System.Collections.Generic;
namespace InventorySolver
{
    public static class ShapeRotator
    {
        public static List<Cell>Rotate(Shape shape,ERotation rotation)
        {
            if(shape==null)throw new ArgumentNullException(nameof(shape));
            var result=new List<Cell>(shape.Cells.Count);
            foreach(Cell cell in shape.Cells)
                result.Add(RotateCell(cell,rotation));
            return ShapeNormalizer.Normalize(result);
        }
        //只做初步象限转换
        //再归一化处理
        private static Cell RotateCell(Cell cell,ERotation rotation)
        {
            return rotation switch
            {
                ERotation.R0 => cell,
                ERotation.R90 => new Cell(-cell.Y, cell.X),
                ERotation.R180 => new Cell(-cell.X, -cell.Y),
                ERotation.R270 => new Cell(cell.Y, -cell.X),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public static ShapeRotationSet Build(Shape shape,bool canRotate)
        {
            if(shape==null)throw new ArgumentNullException(nameof(shape));
            var directions=new ShapeVariant[4];
            var unique=new List<ShapeVariant>();
            int count=canRotate?4:1;
            for(int i=0;i<4;i++)
            {
                ERotation rotation=(ERotation)i;
                if(i>=count)
                {
                    directions[i]=directions[0];
                    continue;
                }
                List<Cell> cells=Rotate(shape,rotation);
                cells.Sort(CompareCells);
                ShapeVariant variant=FindSame(unique,cells);
                if(variant==null)
                {
                    variant=new ShapeVariant(rotation,GetWidth(cells),GetHeight(cells),cells.ToArray());
                    unique.Add(variant);
                }
                directions[i]=variant;
            }
            return new ShapeRotationSet(directions,unique.AsReadOnly());
        }

        private static ShapeVariant FindSame(IReadOnlyList<ShapeVariant>variants,IReadOnlyList<Cell>cells)
        {
            foreach(ShapeVariant variant in variants)
                if(AreSameShape(variant.Cells,cells))return variant;
            return null;
        }

        private static bool AreSameShape(IReadOnlyList<Cell>a,IReadOnlyList<Cell>b)
        {
            if(a.Count!=b.Count)return false;
            for(int i=0;i<a.Count;i++)
                if(a[i].X!=b[i].X||a[i].Y!=b[i].Y)return false;
            return true;
        }
        private static int GetWidth(IReadOnlyList<Cell> cells)
        {
            int max=0;
            foreach(Cell cell in cells)if(cell.X>max)max=cell.X;
            return max+1;
        }
        private static int GetHeight(IReadOnlyList<Cell> cells)
        {
            int max=0;
            foreach(Cell cell in cells)if(cell.Y>max)max=cell.Y;
            return max+1;
        }

        private static int CompareCells(Cell a,Cell b)
        {
            int y=a.Y.CompareTo(b.Y);
            return y!=0?y:a.X.CompareTo(b.X);
        }
        
    }
}
