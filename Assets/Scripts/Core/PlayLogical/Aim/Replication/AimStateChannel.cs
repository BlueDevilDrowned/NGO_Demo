using Unity.Netcode;
using UnityEngine;

public class AimStateChannel : ActorSycnChannel<AimStateSnapshot>
{
    public AimStateChannel(Actor actor) : base(actor)
    {
    }


    public override SycnDirection direction => SycnDirection.ServerToClients;

    private uint lastReceivedServerTick;
    private bool hasReceivedState;

    public override bool TryApply(uint Tick, FastBufferReader reader, int payloadEnd)
    {
        if(hasReceivedState&&Tick<=lastReceivedServerTick)return false;

        reader.ReadNetworkSerializable(out AimStateSnapshot snapshot);
        if(reader.Position!=payloadEnd||!IsFinite(snapshot.Data.TargetPosition))
            return false;

        actor.simulation.aimData=snapshot.Data;
        //更新客户端不可靠数据
        if(actor.IsOwner)
            actor.aimSystem.data.IsAiming=snapshot.Data.IsAiming;

        lastReceivedServerTick=Tick;
        hasReceivedState=true;
        return true;
    }

    public override bool TryWrite(uint Tick, FastBufferWriter writer)
    {
        AimStateSnapshot snapshot=new()
        {
            Data=actor.simulation.aimData,
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
