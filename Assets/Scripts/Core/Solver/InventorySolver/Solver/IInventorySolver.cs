using System.Collections.Generic;

namespace InventorySolver
{
    public interface IInventorySolver
    {
        PlacementCheckResult CanPlace(InventoryLayout layout, IReadOnlyList<SolverItem> existingItems, SolverItem item, Placement placement);
        SolveResult TryAutoPlace(InventoryLayout layout, IReadOnlyList<SolverItem> existingItems, SolverItem item, SolverOptions options);
    }
}
