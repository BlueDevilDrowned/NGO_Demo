using UnityEngine;
/// <summary>
/// 只提供了客户端同步到服务器，因为客户端相机位置角度由aim状态限制，所以aim同步之后，相机也会自动回正
/// </summary>
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
