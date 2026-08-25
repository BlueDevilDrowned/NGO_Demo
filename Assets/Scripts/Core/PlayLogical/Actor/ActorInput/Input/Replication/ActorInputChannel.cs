using Unity.Netcode;
using UnityEngine.InputSystem;
//操作处理成快照再同步到服务器
public class ActorInputChannel : ActorSycnChannel<ActorInputSnapshot>
{
    public override ushort ChannelId => 1;

    public override SycnDirection direction => SycnDirection.OwnerToServer;

    public ActorInputChannel(Actor actor) : base(actor)
    {
    }

    public override bool TryApply(uint Tick, FastBufferReader reader, int payloadEnd)
    {
        reader.ReadNetworkSerializable(out ActorInputSnapshot snapshot);
        if(reader.Position!=payloadEnd)return false;
        
        return actor.inputSystem.replication.ApplyNetWorkInput(snapshot);

    }

    public override bool TryWrite(uint Tick, FastBufferWriter writer)
    {
        ActorInputData data=actor.inputSystem.replication.BuildData();
        ActorInputSnapshot snapshot = new()
        {
            Tick=Tick,
            Data=data,
        };
        writer.WriteNetworkSerializable(in snapshot);
        return true;
    }


}