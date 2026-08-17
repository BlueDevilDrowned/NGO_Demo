using Unity.Netcode;

public class AimStateChannel : ActorSycnChannel<AimSnapshot>
{
    public AimStateChannel(Actor actor) : base(actor)
    {
    }


    public override ushort ChannelId => 3;

    public override SycnDirection direction => SycnDirection.ServerToClients;

    private uint LastReceivedServerTick;
    private bool haveReceived=false;

    public override bool TryApply(uint Tick, FastBufferReader reader, int payloadEnd)
    {
        reader.ReadNetworkSerializable(out AimSnapshot snapshot);
        //同步权威板和预测板
        //tick处理，只接受大于上一次接收的服务器tick

        if(haveReceived&&LastReceivedServerTick>=Tick)return false;
        if(!haveReceived)haveReceived=true;
        actor.simulation.aimData=snapshot.Data;
        actor.aimSystem.data=snapshot.Data;
        LastReceivedServerTick=Tick;
        return true;
    }

    public override bool TryWrite(uint Tick, FastBufferWriter writer)
    {
        //从权威板拿数据
        AimSnapshot snapshot=new();
        snapshot.Tick=Tick;
        snapshot.Data=actor.simulation.aimData;

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

}