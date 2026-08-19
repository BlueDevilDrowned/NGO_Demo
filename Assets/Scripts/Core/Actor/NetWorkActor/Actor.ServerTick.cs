public partial class Actor
{
    private void SeverTick(uint Tick)
    {
        locomotionSystem.ServerTick();
        movement.BeginTick();
        actorStateSystem.ServerTick(Tick);
        upperBodyStateSystem.ServerTick(Tick);
        aimSystem.ServerTick();
        interactSystem.ServerTick();
        weapon.ServerTick(Tick);
        movement.Execute();
    }
}
