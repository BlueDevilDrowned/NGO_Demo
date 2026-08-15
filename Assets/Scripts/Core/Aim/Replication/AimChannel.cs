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
        
    }

    public override bool TryWrite(uint Tick, FastBufferWriter writer)
    {
        
    }

}