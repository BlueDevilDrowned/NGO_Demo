//需要每帧计算角度
//客户端计算角度，发送至服务端校验
using UnityEngine;

public class ActorCameraSystem:IActorOwnershipSystem
{
    public Actor actor;
    public ActorCameraData data;//客户端自己维护，服务器确定是否合理后设置值权威面板
    public ActorCameraReplication replication;
    private CameraViewMode mode;//只用于表现层
    private CameraPerspectiveMode perspectiveMode;
    public ActorCameraRig rig =>ActorCameraRig.Instance;
    public CameraPerspectiveMode PerspectiveMode=>perspectiveMode;
    public bool isDisposed;
    public ActorCameraSystem(Actor actor)
    {
        this.actor=actor;
        data=new();
        mode=CameraViewMode.FreeLook;
        perspectiveMode=CameraPerspectiveMode.ThirdPerson;
        replication=new(actor);
        actor.RegisterSystem(this);
    }
    public void Dispose()
    {
        if(isDisposed)return;
        replication.Dispose();
        rig?.SetPerspectiveMode(CameraPerspectiveMode.ThirdPerson);
        rig?.Unbind(actor.cameraPivot);
        isDisposed=true;

    }

    public void OnGainedOwnership()
    {
        if(!actor.IsOwner)return;

        ActorCameraRig cameraRig=rig;
        if(cameraRig==null)return;

        EnsureRigBindings(cameraRig);
        ApplyPerspectiveMode(cameraRig);

    }

    public void OnLostOwnership()
    {
        ActorCameraRig cameraRig=rig;
        cameraRig?.SetPerspectiveMode(CameraPerspectiveMode.ThirdPerson);
        cameraRig?.Unbind(actor.cameraPivot);
    }

    /// <summary>
    /// 设置相机透视模式
    /// </summary>
    /// <param name="nextMode">要设置的下一个透视模式</param>
    /// <returns>设置成功返回true，失败返回false</returns>
    public bool ApplyPerspectiveMode(CameraPerspectiveMode nextMode)
    {
        // 检查对象是否已被释放或当前玩家是否不是所有者
        if(isDisposed||!actor.IsOwner)return false;

        // 如果设置为一人称模式，但未配置一相机目标点，则报错并返回
        if(nextMode==CameraPerspectiveMode.FirstPerson&&
           actor.firstCameraPivot==null)
        {
            Debug.LogError(
                "First-person camera target is not configured.",
                actor);
            return false;
        }

        // 获取相机装配体，如果为空则直接返回true
        ActorCameraRig cameraRig=rig;
        if(cameraRig==null)
        {
            perspectiveMode=nextMode;
            return true;
        }

        // 确保相机装配体的绑定关系正确
        EnsureRigBindings(cameraRig);
        // 应用新的透视模式
        cameraRig.SetPerspectiveMode(nextMode);
        perspectiveMode=nextMode;
        return true;
    }
    //表现层更新角度（只有owner可以）
    public void PresentationUpdate(float deltaTime)
    {
        if(isDisposed||!actor.IsOwner)return;

        ActorCameraRig cameraRig=rig;
        if(cameraRig==null)return;
        EnsureRigBindings(cameraRig);
        if(!ApplyPerspectiveMode(cameraRig))return;

        CameraSO config=actor.actorSO.cameraSO;
        if(config==null)return;

        //用非权威输入因为作为表现层是包含预测的，所以先使用非权威
        LocalInputState input=actor.inputSystem.playerController.Input;
        bool firstPerson=
            perspectiveMode==CameraPerspectiveMode.FirstPerson;

        float yawDelta;
        float pitchDelta;
        //移动角度计算
        if(input.LookIsPointerDelta)
        {
            yawDelta=
                input.InputLook.x*
                (firstPerson
                    ?config.FirstPersonPointerYawSensitivity
                    :config.PointerYawSensitivity);

            pitchDelta=
                input.InputLook.y*
                (firstPerson
                    ?config.FirstPersonPointerPitchSensitivity
                    :config.PointerPitchSensitivity);
                    
        }
        else
        {
            //属于摇杆输入（手柄）
            yawDelta =
            input.InputLook.x *
            (firstPerson
                ?config.FirstPersonStickYawDegreesPerSecond
                :config.StickYawDegreesPerSecond) *
            deltaTime;

            pitchDelta =
                input.InputLook.y *
                (firstPerson
                    ?config.FirstPersonStickPitchDegreesPerSecond
                    :config.StickPitchDegreesPerSecond) *
                deltaTime;
        }

        data.ViewYaw=Mathf.Repeat(data.ViewYaw+yawDelta,360f);
        if(firstPerson)
        {
            float bodyYaw=actor.transform.eulerAngles.y;
            float relativeYaw=Mathf.DeltaAngle(bodyYaw,data.ViewYaw);
            relativeYaw=Mathf.Clamp(
                relativeYaw,
                config.FirstPersonMinYaw,
                config.FirstPersonMaxYaw);
            data.ViewYaw=Mathf.Repeat(bodyYaw+relativeYaw,360f);
        }

        CameraViewMode rigMode=actor.aimSystem.IsAiming
            ?CameraViewMode.Aim
            :CameraViewMode.FreeLook;
        mode=rigMode;

        bool isAiming=mode==CameraViewMode.Aim;
        //本地做角度限制，但是不影响逻辑上的限制，逻辑上实际角度由服务器决定
        float minPitch=firstPerson
            ?config.FirstPersonMinPitch
            :isAiming
                ?config.AimMinPitch
                :config.FreeLookMinPitch;

        float maxPitch=firstPerson
            ?config.FirstPersonMaxPitch
            :isAiming
                ?config.AimMaxPitch
                :config.FreeLookMaxPitch;

        data.ViewPitch=Mathf.Clamp(data.ViewPitch-pitchDelta,minPitch,maxPitch);

        cameraRig.ApplyView(in data);
        cameraRig.SetViewMode(rigMode);

        //更新位置角度
        Transform output=cameraRig.OutputTransform;
        if(output==null)return;

        data.ViewOrigin=output.position;
        data.ViewDirection=output.forward;
    }

    private void EnsureRigBindings(ActorCameraRig cameraRig)
    {
        if(!cameraRig.IsBoundTo(actor.cameraPivot))
            cameraRig.Bind(actor.cameraPivot);

        if(actor.firstCameraPivot!=null&&
           !cameraRig.IsFirstPersonTarget(actor.firstCameraPivot))
        {
            cameraRig.SetFirstPersonTarget(actor.firstCameraPivot);
        }
    }

    private bool ApplyPerspectiveMode(ActorCameraRig cameraRig)
    {
        cameraRig.SetPerspectiveMode(perspectiveMode);
        return true;
    }

}
