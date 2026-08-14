using UnityEngine;

public class ActorCameraReplication:IActorSystem
{
    public ActorCameraChannel channel;
    public Actor actor;
    
    public ActorCameraReplication(Actor actor)
    {
        this.actor=actor;
        channel=new(actor);
        channel.Register();
    }
    public bool isDisposed=false;
    public void Dispose()
    {
        if(isDisposed)return;
        channel.Unregister();
        isDisposed=true;

    }
}
