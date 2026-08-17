using UnityEngine;

public class AimReplication:IActorSystem
{
    public Actor actor;
    public AimIntentChannel intent;
    public AimStateChannel state;
    public AimReplication(Actor actor)
    {
        this.actor=actor;
        intent=new(actor);
        intent.Register();

        state=new(actor);
        state.Register();
    }
    
    bool isDisposed=false;
    public void Dispose()
    {
        if(isDisposed)return;
        isDisposed=true;
        intent.Unregister();
        state.Unregister();
    }
}
