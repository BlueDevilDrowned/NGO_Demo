//需要每帧计算角度
//客户端计算角度，发送至服务端校验
using UnityEngine;

public class ActorCameraSystem:IActorOwnershipSystem
{
    public Actor actor;
    public ActorCameraData data;//客户端自己维护，服务器确定是否合理后设置值权威面板
    public ActorCameraReplication replication;
    private CameraViewMode mode;//只用于表现层
    public ActorCameraRig rig =>ActorCameraRig.Instance;
    public bool isDisposed;
    public ActorCameraSystem(Actor actor)
    {
        this.actor=actor;
        data=new();
        mode=CameraViewMode.FreeLook;
        replication=new(actor);
        actor.RegisterSystem(this);
    }
    public void Dispose()
    {
        if(isDisposed)return;
        replication.Dispose();
        rig.Unbind(actor.cameraPivot);
        isDisposed=true;

    }

    public void OnGainedOwnership()
    {
        if(!actor.IsOwner)return;
        rig.Bind(actor.cameraPivot);

    }

    public void OnLostOwnership()
    {
        rig.Unbind(actor.cameraPivot);
    }
    //表现层更新角度（只有owner可以）
    public void PresentationUpdate(float deltaTime)
    {
        if(isDisposed||!actor.IsOwner)return;

        //用非权威输入因为作为表现层是包含预测的，所以先使用非权威
        InputIntent input=actor.inputSystem.playerController.Input;

        float yawDelta;
        float pitchDelta;
        //移动角度计算
        if(input.LookIsPointerDelta)
        {
            yawDelta=
                input.InputLook.x*
                actor.actorSO.cameraSO.PointerYawSensitivity;

            pitchDelta=
                input.InputLook.y*
                actor.actorSO.cameraSO.PointerPitchSensitivity;
                    
        }
        else
        {
            //属于摇杆输入（手柄）
            yawDelta =
            input.InputLook.x *
            actor.actorSO.cameraSO.StickYawDegreesPerSecond *
            deltaTime;

            pitchDelta =
                input.InputLook.y *
                actor.actorSO.cameraSO.StickPitchDegreesPerSecond *
                deltaTime;
        }

        data.ViewYaw=Mathf.Repeat(data.ViewYaw+yawDelta,360f);
        
        bool isAiming=mode==CameraViewMode.Aim;
        //本地做角度限制，但是不影响逻辑上的限制，逻辑上实际角度由服务器决定
        float minPitch = isAiming
        ? actor.actorSO.cameraSO.AimMinPitch
        : actor.actorSO.cameraSO.FreeLookMinPitch;

        float maxPitch = isAiming
        ? actor.actorSO.cameraSO.AimMaxPitch
        : actor.actorSO.cameraSO.FreeLookMaxPitch;

        data.ViewPitch=Mathf.Clamp(data.ViewPitch-pitchDelta,minPitch,maxPitch);

        rig.ApplyView(in data);
        //根据不权威是否右键切换相机模式
        //注意之后要修改，不能只是用右键判断，之后要用aim系统的非权威aim
        CameraViewMode rigMode=actor.aimSystem.IsAiming?CameraViewMode.Aim:CameraViewMode.FreeLook;
        rig.SetViewMode(rigMode);
        mode=rigMode;
    }

}