using System.Collections.Generic;

namespace InventorySolver
{
    public sealed class SolvedItem
    {
        public int Token { get; }
        public Placement Placement { get; }
        public bool IsMoved { get; }
        public bool IsTarget { get; }
        public SolvedItem(int token, Placement placement, bool isMoved, bool isTarget) { Token=token; Placement=placement; IsMoved=isMoved; IsTarget=isTarget; }
    }

    public sealed class SolveResult
    {
        public bool Success { get; }
        public IReadOnlyList<SolvedItem> Items { get; }
        public int NodesVisited { get; }
        public SolveResult(bool success, IReadOnlyList<SolvedItem> items, int nodesVisited) { Success=success; Items=items; NodesVisited=nodesVisited; }
    }
}
