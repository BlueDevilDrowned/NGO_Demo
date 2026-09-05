using Unity.Netcode;

public sealed class WeaponInventoryChannel
    : ActorSycnChannel<WeaponInventorySnapshot>
{
    private readonly WeaponInventoryReplication replication;
    private bool hasReceivedState;
    private uint lastReceivedTick;

    public WeaponInventoryChannel(
        Actor actor,
        WeaponInventoryReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override SycnDirection direction=>SycnDirection.ServerToClients;

    public override bool TryWrite(
        uint tick,
        FastBufferWriter writer)
    {
        if(!replication.TryBuildState(
               out WeaponInventorySnapshot snapshot))
            return false;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    public override bool TryApply(
        uint tick,
        FastBufferReader reader,
        int payloadEnd)
    {
        if(actor.IsServer||hasReceivedState&&tick<=lastReceivedTick)
            return false;

        reader.ReadNetworkSerializable(
            out WeaponInventorySnapshot snapshot);
        if(reader.Position!=payloadEnd||!IsValid(in snapshot))
            return false;

        replication.ReceiveState(in snapshot);
        hasReceivedState=true;
        lastReceivedTick=tick;
        return true;
    }

    private static bool IsValid(
        in WeaponInventorySnapshot snapshot)
    {
        if(snapshot.SlotCount>WeaponInventorySnapshot.MaxSlots||
           snapshot.CurrentIndex>=snapshot.SlotCount&&
           snapshot.SlotCount>0)
            return false;

        for(int i=0;i<snapshot.SlotCount;i++)
        {
            ushort weaponId=snapshot.GetSlot(i);
            if(weaponId==0)continue;
            if(!WeaponCatalog.TryGet(weaponId,out _))return false;
        }

        return snapshot.SlotCount>0||snapshot.CurrentIndex==0;
    }
}
