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
        Transform logicalView=actor.firstCameraPivot;
        CameraSO config=actor.actorSO.cameraSO;
        if(logicalView==null||config==null||
           !ActorCameraDataUtility.IsFinite(spanshot.data.ViewYaw)||
           !ActorCameraDataUtility.IsFinite(spanshot.data.ViewPitch))
            return false;

        ActorCameraData cameraData=spanshot.data;
        float bodyYaw=actor.transform.eulerAngles.y;
        float relativeYaw=Mathf.DeltaAngle(bodyYaw,cameraData.ViewYaw);
        relativeYaw=Mathf.Clamp(
            relativeYaw,
            config.FirstPersonMinYaw,
            config.FirstPersonMaxYaw);
        cameraData.ViewYaw=Mathf.Repeat(bodyYaw+relativeYaw,360f);
        cameraData.ViewPitch=Mathf.Clamp(
            cameraData.ViewPitch,
            config.FirstPersonMinPitch,
            config.FirstPersonMaxPitch);
        cameraData.ViewOrigin=logicalView.position;
        cameraData.ViewDirection=ActorCameraDataUtility.CalculateViewDirection(
            cameraData.ViewYaw,
            cameraData.ViewPitch);
        actor.simulation.cameraData=cameraData;
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
