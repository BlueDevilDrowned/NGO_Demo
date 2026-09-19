using Unity.Netcode;
using UnityEngine;

public sealed class AimStateChannel : ActorSycnChannel<AimTargetStateSnapshot>
{
    private readonly AimReplication replication;
    public AimStateChannel(Actor actor, AimReplication replication) : base(actor)
    {
        this.replication=replication;
    }


    public override SycnDirection direction => SycnDirection.ServerToClients;
    public override SyncDataKind DataKind=>SyncDataKind.ContinuousState;
    public override SyncSchedule Schedule=>SyncSchedule.EveryTick;

    private uint lastReceivedServerTick;
    private bool hasReceivedState;

    public override bool TryApply(uint Tick, FastBufferReader reader, int payloadEnd)
    {
        if(hasReceivedState&&Tick<=lastReceivedServerTick)return false;

        reader.ReadNetworkSerializable(out AimTargetStateSnapshot snapshot);
        if(reader.Position!=payloadEnd||!IsFinite(snapshot.TargetPosition))
            return false;

        actor.actorSyncSystem.History.Push(this,Tick,in snapshot);
        replication.ReceiveTargetState(in snapshot);

        lastReceivedServerTick=Tick;
        hasReceivedState=true;
        return true;
    }

    public override bool TryWrite(uint Tick, FastBufferWriter writer)
    {
        if(!actor.IsServer)
            return false;

        AimTargetStateSnapshot snapshot=new()
        {
            TargetPosition=actor.simulation.aimData.TargetPosition,
        };

        writer.WriteNetworkSerializable(snapshot);
        return true;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&
               !float.IsNaN(value.z)&&!float.IsInfinity(value.z);
    }

}
