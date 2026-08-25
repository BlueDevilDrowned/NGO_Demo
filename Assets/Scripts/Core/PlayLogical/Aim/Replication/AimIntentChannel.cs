using Unity.Netcode;
/// <summary>
/// 客户端上传aim意图
/// </summary>
public class AimIntentChannel : ActorSycnChannel<AimIntentSnapshot>
{
    public AimIntentChannel(Actor actor) : base(actor)
    {
    }


    public override ushort ChannelId => 3;

    public override SycnDirection direction => SycnDirection.OwnerToServer;
    private uint lastReceivedIntentTick;
    private bool hasReceivedIntent;

    public override bool TryApply(uint Tick, FastBufferReader reader, int payloadEnd)
    {
        //接收tick大于上次接收的tick
        if(hasReceivedIntent&&Tick<=lastReceivedIntentTick)return false;

        reader.ReadNetworkSerializable(out AimIntentSnapshot snapshot);
        if(reader.Position!=payloadEnd)return false;

        actor.simulation.aimData.IsAiming=snapshot.IsAiming;
        lastReceivedIntentTick=Tick;
        hasReceivedIntent=true;
        return true;
    }

    public override bool TryWrite(uint Tick, FastBufferWriter writer)
    {
        AimIntentSnapshot snapshot=new()
        {
            IsAiming=actor.aimSystem.data.IsAiming,
        };

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

}
