public partial class Actor
{
    private void OwnerTick(uint tick)
    {
        perspectiveSystem.OwnerTick(tick);
        weapon.OwnerTick(tick);
    }
}
