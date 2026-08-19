public partial class Actor
{
    private void SeverTick(uint Tick)
    {
        aimSystem.ServerTick();
        weapon.ServerTick(Tick);
    }
}
