using Unity.Netcode;
using UnityEngine;

public class ActorCameraChannel : ActorSycnChannel<ActorCameraSpanshot>
{
    public ActorCameraChannel(Actor actor) : base(actor)
    {
    }

    public override ushort ChannelId => 2;

    public override SycnDirection direction => SycnDirection.OwnerToServer;
    //注意目前没有对数据做保护

    public override bool TryApply(uint Tick, FastBufferReader reader, int payloadEnd)
    {
        //记得根据权威服务器的角度限制来决定数据
        reader.ReadNetworkSerializable(out ActorCameraSpanshot spanshot);
        actor.simulation.cameraData=spanshot.data;
        return true;
    }

    public override bool TryWrite(uint Tick, FastBufferWriter writer)
    {
        ActorCameraSpanshot spanshot=new()
        {
            data=actor.cameraSystem.data,
        };

        writer.WriteNetworkSerializable(spanshot);
        return true;
    }
}
