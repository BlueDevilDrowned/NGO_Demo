public partial class Actor
{
    private void PresentationUpdate(float deltaTime)
    {
        if(actorStateSystem==null||upperBodyStateSystem==null)return;
        animationArbiter?.Tick();
        actorStateSystem.PresentationUpdate(deltaTime);
        upperBodyStateSystem.PresentationUpdate(deltaTime);
        if(IsOwner)
            firstPersonStateSystem.PresentationUpdate(deltaTime);
    }
}
