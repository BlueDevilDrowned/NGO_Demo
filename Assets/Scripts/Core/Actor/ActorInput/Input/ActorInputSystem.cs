public sealed class ActorInputSystem : IActorOwnershipSystem
{
    public Actor actor;
    public readonly NetWorkPlayerController playerController;
    public ActorInputReplication replication;

    private bool isDisposed;

    public ActorInputSystem(Actor actor)
    {
        this.actor=actor;
        playerController=new();
        replication=new(actor);

        if(actor.IsClient&&actor.IsOwner)
            playerController.EnableInput();

        actor.RegisterSystem(this);
    }

    public void OnGainedOwnership()
    {
        if(!isDisposed&&actor.IsClient&&actor.IsOwner)
            playerController.EnableInput();
    }

    public void OnLostOwnership()
    {
        playerController.DisableInput();
    }

    public void Dispose()
    {
        if(isDisposed)return;

        playerController.DisableInput();
        replication.Dispose();
        playerController.Dispose();
        isDisposed=true;
    }
}
