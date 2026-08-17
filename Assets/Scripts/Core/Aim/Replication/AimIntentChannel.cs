using Unity.Netcode;

public class AimIntentChannel : ActorSycnChannel<AimSnapshot>
{
    public AimIntentChannel(Actor actor) : base(actor)
    {
    }


    public override ushort ChannelId => 3;

    public override SycnDirection direction => SycnDirection.OwnerToServer;
    private uint LastReceivedIntentTick;
    private bool haveReceived=false;

    public override bool TryApply(uint Tick, FastBufferReader reader, int payloadEnd)
    {
        reader.ReadNetworkSerializable(out AimSnapshot snapshot);
        //
        if(haveReceived&&LastReceivedIntentTick>=Tick)return false;
        if(!haveReceived)haveReceived=true;
        actor.simulation.aimData=snapshot.Data;
        LastReceivedIntentTick=Tick;
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