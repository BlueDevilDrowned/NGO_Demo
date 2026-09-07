namespace InventorySolver
{
    /// <summary>
    /// 放置方式，位置，旋转
    /// </summary>
    public readonly struct Placement
    {
        public readonly int RegionIndex;
        public readonly Cell Anchor;
        public readonly ERotation Rotation;

        public Placement(
            int regionIndex,
            Cell anchor,
            ERotation rotation)
        {
            RegionIndex = regionIndex;
            Anchor = anchor;
            Rotation = rotation;
        }
    }
}
