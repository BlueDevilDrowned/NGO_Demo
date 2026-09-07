using System;

namespace InventorySolver
{
    public readonly struct RegionCell : IEquatable<RegionCell>
    {
        public readonly int RegionIndex;
        public readonly int X;
        public readonly int Y;

        public RegionCell(int regionIndex, int x, int y)
        {
            RegionIndex = regionIndex;
            X = x;
            Y = y;
        }

        public bool Equals(RegionCell other)
        {
            return RegionIndex == other.RegionIndex &&
                X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is RegionCell && Equals((RegionCell)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = RegionIndex;
                hash = hash * 31 + X;
                return hash * 31 + Y;
            }
        }
    }
}
