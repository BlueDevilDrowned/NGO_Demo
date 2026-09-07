using System.Collections.Generic;

namespace InventorySolver
{
    public sealed class ShapeRotationSet
    {
        public IReadOnlyList<ShapeVariant> Directions{get;}
        public IReadOnlyList<ShapeVariant>UniqueVariants{get;}

        public ShapeRotationSet(ShapeVariant[] directions,IReadOnlyList<ShapeVariant>uniqueVariants)
        {
            if(directions==null||directions.Length!=4)
                throw new System.ArgumentException("Four directions are required.",nameof(directions));
            if(uniqueVariants==null)
                throw new System.ArgumentNullException(nameof(uniqueVariants));
            Directions=System.Array.AsReadOnly((ShapeVariant[])directions.Clone());
            UniqueVariants=uniqueVariants;
        }

        public ShapeVariant Get(ERotation rotation)
        {
            return Directions[(int)rotation];
        }
    }
}
