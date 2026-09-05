using Unity.Netcode;

/// <summary>
/// 将服务器产生的射击事件批量广播给所有客户端。
/// </summary>
public sealed class WeaponReplicationChannel
    : ActorSycnChannel<WeaponSnapshot>
{
    private readonly WeaponReplication replication;
    private uint lastReceivedServerTick;
    private bool hasReceivedSnapshot;

    public override SycnDirection direction=>SycnDirection.ServerToClients;

    public WeaponReplicationChannel(
        Actor actor,
        WeaponReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override bool TryWrite(uint Tick,FastBufferWriter writer)
    {
        //服务器事件同步到客户端表现层
        if(!replication.TryBuildSnapshot(out WeaponSnapshot snapshot))
            return false;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    public override bool TryApply(
        uint Tick,
        FastBufferReader reader,
        int payloadEnd)
    {
        if(hasReceivedSnapshot&&Tick<=lastReceivedServerTick)return false;

        reader.ReadNetworkSerializable(out WeaponSnapshot snapshot);
        if(reader.Position!=payloadEnd||
           !replication.TryReceiveSnapshot(in snapshot))return false;

        lastReceivedServerTick=Tick;
        hasReceivedSnapshot=true;
        return true;
    }
}
