//负责整合接口，并注册进同步系统，tick更新
using Unity.Netcode;

public abstract class ActorSycnChannel<T> : IActorSycnChannel
{
    //adapter负责实现逻辑，与数据对应
    public abstract ushort ChannelId{get;}
    private IReplicationAdapter<T>adapter;
    private Actor actor;
    public SycnDirection direction;
    //自己加上数据
    public abstract bool TryWrite(FastBufferWriter writer);

    public abstract bool TryApply(FastBufferReader reader,int payloadEnd);
    public virtual void Initial(Actor actor)
    {
        //适配器初始化
        this.actor=actor;
    }

    public  void Register()
    {
        actor.actorSyncSystem.Register(ChannelId,direction,this);
    }
    public  void UnRegister()
    {
        actor.actorSyncSystem.UnRegister(ChannelId,direction);
    }

}