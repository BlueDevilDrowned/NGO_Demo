using Unity.Netcode;

public class AimChannel : ActorSycnChannel<AimSnapshot>
{
    public AimChannel(Actor actor) : base(actor)
    {
    }


    public override ushort ChannelId => 3;

    public override SycnDirection direction => SycnDirection.OwnerToServer;

    public override bool TryApply(uint Tick, FastBufferReader reader, int payloadEnd)
    {
        reader.ReadNetworkSerializable(out AimSnapshot snapshot);
        //
        actor.simulation.aimData=snapshot.Data;
        return true;
    }

    public override bool TryWrite(uint Tick, FastBufferWriter writer)
    {
        AimSnapshot snapshot=new();
        snapshot.Tick=Tick;
        snapshot.Data=actor.aimSystem.data;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

}