//需要每帧计算角度
//客户端计算角度，发送至服务端校验
using UnityEngine;

public class ActorCameraSystem:IActorOwnershipSystem
{
    public Actor actor;
    public ActorCameraData data;//当前有效相机数据，包含已应用的表现偏移
    public ActorCameraReplication replication;
    public CameraArbiter Arbiter{get;}
    private ActorCameraData virtualData;//不包含表现偏移的玩家逻辑视角
    private CameraViewMode mode;//只用于表现层
    private CameraPerspectiveMode perspectiveMode;
    public ActorCameraRig rig =>ActorCameraRig.Instance;
    public CameraPerspectiveMode PerspectiveMode=>perspectiveMode;
    public bool isDisposed;
    public ActorCameraSystem(Actor actor)
    {
        this.actor=actor;
        float initialYaw=actor.transform.eulerAngles.y;
        data=new ActorCameraData
        {
            ViewYaw=initialYaw,
            ViewPitch=0f,
            ViewOrigin=actor.firstCameraPivot!=null
                ?actor.firstCameraPivot.position
                :actor.transform.position,
            ViewDirection=ActorCameraDataUtility.CalculateViewDirection(
                initialYaw,
                0f),
        };
        virtualData=data;
        if(actor.IsServer)
            actor.simulation.cameraData=data;
        mode=CameraViewMode.FreeLook;
        perspectiveMode=CameraPerspectiveMode.ThirdPerson;
        replication=new(actor);
        Arbiter=new();
        actor.RegisterSystem(this);
    }

    public bool Submit(in CameraRotationRequest request)
    {
        if(isDisposed||!actor.IsOwner)return false;

        Arbiter.Submit(in request);
        return true;
    }

    public bool SubmitRecoil(in CameraRecoilRequest request)
    {
        if(isDisposed||!actor.IsOwner)return false;

        Arbiter.SubmitRecoil(in request);
        return true;
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

        // 无论输出哪台相机，玩法逻辑始终使用第一人称视点。
        if(actor.firstCameraPivot==null)
        {
            Debug.LogError(
                "Logical first-person camera target is not configured.",
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
        float yawDelta;
        float pitchDelta;
        //移动角度计算
        if(input.LookIsPointerDelta)
        {
            yawDelta=
                input.InputLook.x*
                config.FirstPersonPointerYawSensitivity;

            pitchDelta=
                input.InputLook.y*
                config.FirstPersonPointerPitchSensitivity;
                    
        }
        else
        {
            //属于摇杆输入（手柄）
            yawDelta =
            input.InputLook.x *
            config.FirstPersonStickYawDegreesPerSecond *
            deltaTime;

            pitchDelta =
                input.InputLook.y *
                config.FirstPersonStickPitchDegreesPerSecond *
                deltaTime;
        }

        CameraRotationRequest request=new(
            "LookInput",
            yawDelta,
            -pitchDelta);
        Arbiter.Submit(in request);

        CameraViewMode rigMode=actor.aimSystem.IsAiming
            ?CameraViewMode.Aim
            :CameraViewMode.FreeLook;
        mode=rigMode;

        ActorCameraData appliedData=Arbiter.Resolve(
            ref virtualData,
            config,
            deltaTime);

        data=appliedData;
        cameraRig.ApplyView(in appliedData);
        cameraRig.SetViewMode(rigMode);

        // 逻辑视角和当前有效视角都使用第一人称视点位置。
        virtualData.ViewOrigin=actor.firstCameraPivot.position;
        virtualData.ViewDirection=ActorCameraDataUtility.CalculateViewDirection(
            virtualData.ViewYaw,
            virtualData.ViewPitch);
        data.ViewOrigin=virtualData.ViewOrigin;
        data.ViewDirection=ActorCameraDataUtility.CalculateViewDirection(
            data.ViewYaw,
            data.ViewPitch);
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
