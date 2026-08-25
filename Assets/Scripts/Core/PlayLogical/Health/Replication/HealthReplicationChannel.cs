using Unity.Netcode;

public sealed class HealthReplicationChannel
    : ActorSycnChannel<HealthSnapshot>
{
    public const ushort Id=7;

    private readonly HealthReplication replication;
    private bool hasReceivedState;
    private uint lastReceivedTick;

    public HealthReplicationChannel(
        Actor actor,
        HealthReplication replication) : base(actor)
    {
        this.replication=replication;
    }

    public override ushort ChannelId=>Id;
    public override SycnDirection direction=>SycnDirection.ServerToClients;

    public override bool TryWrite(uint tick,FastBufferWriter writer)
    {
        if(!replication.TryBuildState(out HealthSnapshot snapshot))
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

        reader.ReadNetworkSerializable(out HealthSnapshot snapshot);
        if(reader.Position!=payloadEnd||!IsValid(in snapshot))return false;

        replication.ReceiveState(snapshot);
        hasReceivedState=true;
        lastReceivedTick=tick;
        return true;
    }

    private static bool IsValid(in HealthSnapshot snapshot)
    {
        return IsFinite(snapshot.CurrentHealth)&&
               IsFinite(snapshot.MaxHealth)&&
               snapshot.CurrentHealth>=0f&&
               snapshot.MaxHealth>=1f&&
               snapshot.CurrentHealth<=snapshot.MaxHealth;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
