namespace InventorySolver
{
    public sealed class SolverOptions
    {
        /// <summary>
        /// 没此变成一个子问题都是一个node，这个是限定搜索的总node
        /// </summary>
        public int MaxNodes { get; set; } = 10000;
        /// <summary>
        /// Depth是侯选位置覆盖多个物品时，每个物品都是一个Depth，一个子问题
        /// </summary>
        public int MaxDepth { get; set; } = 32;
    }
}
