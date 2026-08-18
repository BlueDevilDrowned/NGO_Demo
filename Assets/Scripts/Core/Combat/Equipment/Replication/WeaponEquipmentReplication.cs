public sealed class WeaponEquipmentReplication : IActorSystem
{
    private readonly Actor actor;
    private readonly WeaponEquipmentChannel channel;
    private bool stateDirty;
    private bool hasReceivedState;
    private WeaponEquipmentSnapshot state;
    private bool isDisposed;

    public WeaponEquipmentReplication(Actor actor)
    {
        this.actor=actor;
        channel=new(actor,this);
        channel.Register();
        stateDirty=actor.IsServer;
        state=new WeaponEquipmentSnapshot
        {
            data=actor.simulation.weaponEquipmentData,
        };
    }
    /// <summary>
    /// 同步服务器的操作
    /// </summary>
    /// <param name="data"></param>
    /// <param name="processedInputTick"></param>
    public void MarkAuthoritativeState(
        in WeaponEquipmentData data,
        uint processedInputTick)
    {
        if(!actor.IsServer)return;

        actor.simulation.weaponEquipmentData=data;
        state=new WeaponEquipmentSnapshot
        {
            ProcessedInputTick=processedInputTick,
            data=data,
        };
        stateDirty=true;
    }

    internal bool TryBuildState(out WeaponEquipmentSnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;

        stateDirty=false;
        return true;
    }
    /// <summary>
    /// 写入客户端的权威数据板
    /// </summary>
    /// <param name="snapshot"></param>
    internal void ReceiveState(in WeaponEquipmentSnapshot snapshot)
    {
        actor.simulation.weaponEquipmentData=snapshot.data;
        state=snapshot;
        hasReceivedState=true;
    }
    /// <summary>
    /// 尝试消费已经接收的快照，消费后hasReceivedState=false
    /// </summary>
    /// <param name="snapshot"></param>
    /// <returns></returns>
    public bool TryConsumeState(out WeaponEquipmentSnapshot snapshot)
    {
        snapshot=state;
        if(!hasReceivedState)return false;

        hasReceivedState=false;
        return true;
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        channel.Unregister();
    }
}
