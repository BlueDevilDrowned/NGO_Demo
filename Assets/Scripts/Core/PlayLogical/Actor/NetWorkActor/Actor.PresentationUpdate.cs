public partial class Actor
{
    private void PresentationUpdate(float deltaTime)
    {
        animationArbiter?.Tick();
        actorStateSystem.PresentationUpdate(deltaTime);
        upperBodyStateSystem.PresentationUpdate(deltaTime);
        firstPersonStateSystem.PresentationUpdate(deltaTime);
    }
}
