using Unity.Netcode;

public sealed class WeaponEquipmentChannel : ActorSycnChannel<WeaponEquipmentSnapshot>
{
    private readonly WeaponEquipmentReplication replication;
    private uint lastReceivedServerTick;
    private bool hasReceivedState;

    public WeaponEquipmentChannel(
        Actor actor,
        WeaponEquipmentReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override ushort ChannelId=>4;
    public override SycnDirection direction=>SycnDirection.ServerToClients;

    public override bool TryApply(
        uint Tick,
        FastBufferReader reader,
        int payloadEnd)
    {
        //只接受>=上次接收的tick
        if(hasReceivedState&&Tick<=lastReceivedServerTick)return false;

        reader.ReadNetworkSerializable(out WeaponEquipmentSnapshot snapshot);
        if(reader.Position!=payloadEnd||!IsValidWeaponId(snapshot.data.id))
            return false;

        replication.ReceiveState(snapshot);
        lastReceivedServerTick=Tick;
        hasReceivedState=true;
        return true;
    }

    public override bool TryWrite(uint Tick,FastBufferWriter writer)
    {
        if(!replication.TryBuildState(out WeaponEquipmentSnapshot snapshot))
            return false;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    private static bool IsValidWeaponId(int weaponId)
    {
        return weaponId==-1||weaponId>0&&weaponId<=ushort.MaxValue;
    }
}
