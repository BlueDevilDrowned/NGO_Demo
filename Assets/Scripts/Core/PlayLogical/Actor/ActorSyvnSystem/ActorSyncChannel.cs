//负责整合接口，并注册进同步系统，tick更新
using Unity.Netcode;

public abstract class ActorSycnChannel<T> : IActorSycnChannel
{
    //adapter负责实现逻辑，与数据对应
    public abstract ushort ChannelId{get;}
    protected Actor actor;
    public abstract SycnDirection direction{get;}
    private bool isRegistered;
    //自己加上数据
    public abstract bool TryWrite(uint Tick,FastBufferWriter writer);

    public abstract bool TryApply(uint Tick,FastBufferReader reader,int payloadEnd);
    public ActorSycnChannel(Actor actor)
    {
        //适配器初始化
        this.actor=actor;
    }

    public void Register()
    {
        if(isRegistered)return;

        actor.actorSyncSystem.Register(ChannelId,direction,this);
        isRegistered=true;
    }

    public void Unregister()
    {
        if(!isRegistered)return;

        actor.actorSyncSystem.UnRegister(ChannelId,direction);
        isRegistered=false;
    }

}
