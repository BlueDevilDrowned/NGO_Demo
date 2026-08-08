using UnityEngine;

public sealed class AimSystem
{
    private readonly Actor actor;

    private bool ownerViewInitialized;
    private float viewYaw;
    private float viewPitch;
    private Vector3 targetPosition;
    private float presentedYaw;
    private float presentedPitch;
    private Vector3 presentedTarget;

    public bool IsActive{get;private set;}

    public AimSystem(Actor actor)
    {
        this.actor=actor;
    }

    public void Active()
    {
        if(IsActive)return;

        IsActive=true;
        ownerViewInitialized=false;
        actor.SetAimMode(true);

        if(!actor.IsOwner)
        {
            //客户端同步服务端的数据
            presentedYaw=actor.runTimeData.aim.ViewYaw;
            presentedPitch=actor.runTimeData.aim.ViewPitch;
            presentedTarget=actor.runTimeData.aim.TargetPosition;
        }

        if(actor.IsServer&&actor.aimRig!=null)
            actor.aimRig.weight=1f;
    }

    public void Deactivate()
    {
        if(!IsActive)return;
        
        IsActive=false;
        ownerViewInitialized=false;
        actor.SetAimMode(false);

        if(actor.IsServer&&!actor.IsClient&&actor.aimRig!=null)
            actor.aimRig.weight=0f;
    }
    //这是表现层更新
    public void PresentationUpdate(float deltaTime)
    {
        //插值更新weight
        UpdateRigWeight(deltaTime);
        if(!IsActive||!actor.IsClient||actor.aimSO==null)return;
        //客户端更新自己的，不是主机就用服务器的数据更新
        if(actor.IsOwner)
            UpdateOwnerView(deltaTime);
        else
            UpdateRemoteView(deltaTime);
    }

    public void CaptureOwnerInput(ref ActorInputData input)
    {
        if(!actor.IsOwner||!IsActive)return;

        input.ViewYaw=viewYaw;
        input.ViewPitch=viewPitch;
        input.AimTargetPosition=targetPosition;
    }

    public void SetOwnerTarget(Vector3 target)
    {
        if(!actor.IsOwner||!IsActive||actor.aimSO==null||
           !IsUsableTarget(target))return;

        targetPosition=target;
        if(actor.aimTarget!=null)
            actor.aimTarget.position=targetPosition;
    }

    public bool TrySubmitBodyTurn(
        float ignoreTurnAngle,
        float maxTurnAngle)
    {
        if(!actor.IsServer||!IsActive)return false;

        float ignoreAngle=Mathf.Clamp(
            Mathf.Abs(ignoreTurnAngle),
            0f,
            180f);
        float maxDelta=Mathf.Clamp(
            Mathf.Abs(maxTurnAngle),
            0f,
            180f);
        if(maxDelta<=Mathf.Epsilon)return false;

        float desiredYaw=actor.runTimeData.Input.ViewYaw;
        float yawError=Mathf.DeltaAngle(
            actor.transform.eulerAngles.y,
            desiredYaw);
        float excessAngle=Mathf.Abs(yawError)-ignoreAngle;
        if(excessAngle<=0f)return false;

        MovementRequest request=MovementRequest.Default;
        request.Source="AimBodyTurn";
        request.YawDelta=Mathf.Sign(yawError)*
                         Mathf.Min(excessAngle,maxDelta);
        actor.movement.Submit(in request);
        return true;
    }

    public void ServerTick()
    {
        if(!actor.IsServer||!IsActive||actor.aimSO==null)return;
        //设置竖直水平角度 
        ActorInputData input=actor.runTimeData.Input;
        float acceptedYaw=Mathf.Repeat(input.ViewYaw,360f);
        float acceptedPitch=actor.aimSO.ClampPitch(input.ViewPitch);
        //检查target是否合法
        Vector3 acceptedTarget=ValidateTarget(
            input.AimTargetPosition,
            acceptedYaw,
            acceptedPitch);

        actor.runTimeData.aim=new AimData
        {
            ViewYaw=acceptedYaw,
            ViewPitch=acceptedPitch,
            TargetPosition=acceptedTarget,
        };

        if(actor.aimRig!=null)
            actor.aimRig.weight=1f;
        ApplyAimPose(acceptedYaw,acceptedPitch,acceptedTarget);
    }

    private void UpdateOwnerView(float deltaTime)
    {
        InitializeOwnerView();
        LocalInputData input=actor.LocalInput;
        if(input==null)return;

        AimRotationSpeed speed=input.LookIsPointerDelta
            ?actor.aimSO.PointerSensitivity
            :actor.aimSO.StickDegreesPerSecond;
        float timeScale=input.LookIsPointerDelta?1f:deltaTime;
        //超过360度去除
        viewYaw=Mathf.Repeat(
            viewYaw+input.InputLook.x*speed.Horizontal*timeScale,
            360f);
        viewPitch=actor.aimSO.ClampPitch(
            viewPitch-input.InputLook.y*speed.Vertical*timeScale);
        //更新摄像机
        ApplyAimPose(viewYaw,viewPitch,targetPosition);
        //更新target位置
        targetPosition=ResolveOwnerTarget();
        ApplyAimPose(viewYaw,viewPitch,targetPosition);
        //设置数据
        actor.runTimeData.aim=new AimData
        {
            ViewYaw=viewYaw,
            ViewPitch=viewPitch,
            TargetPosition=targetPosition,
        };
    }

    private void InitializeOwnerView()
    {
        if(ownerViewInitialized)return;

        Transform view=actor.Cam!=null?actor.Cam:actor.transform;
        viewYaw=view.eulerAngles.y;
        viewPitch=actor.aimSO.ClampPitch(
            Mathf.DeltaAngle(0f,view.eulerAngles.x));
            //初始化时target用备用target
        targetPosition=FallbackTarget(viewYaw,viewPitch);
        presentedYaw=viewYaw;
        presentedPitch=viewPitch;
        presentedTarget=targetPosition;
        ownerViewInitialized=true;
    }

    private void UpdateRemoteView(float deltaTime)
    {
        //用服务端数据平滑角度和target
        AimData target=actor.runTimeData.aim;
        float rotationT=Damping(
            actor.aimSO.RemoteRotationSharpness,
            deltaTime);
        float targetT=Damping(
            actor.aimSO.RemoteTargetSharpness,
            deltaTime);

        presentedYaw=Mathf.LerpAngle(
            presentedYaw,
            target.ViewYaw,
            rotationT);
        presentedPitch=Mathf.Lerp(
            presentedPitch,
            target.ViewPitch,
            rotationT);
        presentedTarget=Vector3.Lerp(
            presentedTarget,
            target.TargetPosition,
            targetT);

        ApplyAimPose(presentedYaw,presentedPitch,presentedTarget);
    }

    private Vector3 ResolveOwnerTarget()
    {
        if(ActorCameraController.Instance!=null&&
           ActorCameraController.Instance.TryGetAimTarget(out Vector3 target)&&
           IsUsableTarget(target))
            return target;

        return FallbackTarget(viewYaw,viewPitch);
    }

    private Vector3 ValidateTarget(
        Vector3 requestedTarget,
        float yaw,
        float pitch)
    {
        return IsUsableTarget(requestedTarget)
            ?requestedTarget
            :FallbackTarget(yaw,pitch);
    }

    private bool IsUsableTarget(Vector3 target)
    {
        if(!IsFinite(target))return false;

        Vector3 origin=actor.aimingCore!=null
            ?actor.aimingCore.position
            :actor.transform.position;
        float maxDistance=Mathf.Max(1f,actor.aimSO.TargetDistance)*1.25f;
        float sqrDistance=(target-origin).sqrMagnitude;
        return sqrDistance>0.0001f&&
               sqrDistance<=maxDistance*maxDistance;
    }
    //备用瞄准点
    private Vector3 FallbackTarget(float yaw,float pitch)
    {
        Vector3 origin=actor.aimingCore!=null
            ?actor.aimingCore.position
            :actor.transform.position;
        Vector3 direction=Quaternion.Euler(pitch,yaw,0f)*Vector3.forward;
        return origin+direction*Mathf.Max(1f,actor.aimSO.TargetDistance);
    }

    private void ApplyAimPose(float yaw,float pitch,Vector3 target)
    {
        if(actor.aimingCore!=null)
            actor.aimingCore.rotation=Quaternion.Euler(pitch,yaw,0f);
        if(actor.aimTarget!=null&&IsFinite(target))
            actor.aimTarget.position=target;
    }

    private void UpdateRigWeight(float deltaTime)
    {
        if(actor.aimRig==null||actor.aimSO==null)return;

        float targetWeight=IsActive?1f:0f;
        actor.aimRig.weight=Mathf.MoveTowards(
            actor.aimRig.weight,
            targetWeight,
            actor.aimSO.RigBlendSpeed*deltaTime);
    }
    //根据剩余值和经过时间算lerp应该平滑的系数
    private static float Damping(float sharpness,float deltaTime)
    {
        return sharpness<=0f
            ?1f
            :1f-Mathf.Exp(-sharpness*deltaTime);
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&
               !float.IsNaN(value.z)&&!float.IsInfinity(value.z);
    }
}
