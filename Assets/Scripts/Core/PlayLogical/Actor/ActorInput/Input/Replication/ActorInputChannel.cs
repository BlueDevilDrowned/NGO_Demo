using Unity.Netcode;
using UnityEngine.InputSystem;
//操作处理成快照再同步到服务器
public class ActorInputChannel : ActorSycnChannel<ActorInputSnapshot>
{
    public override SycnDirection direction => SycnDirection.OwnerToServer;
    public override SyncDataKind DataKind => SyncDataKind.InputFrame;
    public override SyncSchedule Schedule => SyncSchedule.EveryTick;

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
        // 输入的 Tick 是发送时刻；PresentedServerTick 是玩家实际看到的延迟表现时刻。
        // 两者有意分开，服务器可据此对射击进行历史 Tick 回溯。
        ActorInputSnapshot snapshot = new()
        {
            Tick=Tick,
            EstimatedServerTick=actor.serverTick,
            PresentedServerTick=actor.actorSyncSystem.Clock.HasServerClock
                ?actor.actorSyncSystem.Clock.GetDisplayedServerTick(actor.localTick)
                :actor.serverTick,
            Data=data,
        };
        writer.WriteNetworkSerializable(in snapshot);
        return true;
    }


}
