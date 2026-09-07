namespace InventorySolver
{
    public enum PlacementFailureReason { None, InvalidRegion, OutOfBounds, InvalidCell, Occupied }
    public readonly struct PlacementCheckResult
    {
        public readonly bool CanPlace;
        public readonly PlacementFailureReason Reason;
        public PlacementCheckResult(bool canPlace, PlacementFailureReason reason) { CanPlace = canPlace; Reason = reason; }
    }
}
