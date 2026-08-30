using UnityEngine;


/// <summary>
/// 本地客户端自己算target，并负责表现；其他客户端用服务器算的target
/// </summary>
public class AimSystem:IActorSystem
{
    public Actor actor;
    public AimReplication replication;
    public AimSystem(Actor actor)
    {
        this.actor=actor;
        replication=new(actor);
        actor.RegisterSystem(this);
    }

    /// <summary>
    /// 表现层数据，客户端维护，服务器仲裁
    /// </summary>
    public AimData data;
    public bool IsAiming=>data.IsAiming;
    /// <summary>
    /// 客户端修改aim再由服务器决定是否接收，纠正状态
    /// </summary>
    /// <param name="ifAim"></param>
    public void SetPresentationAim(bool ifAim)
    {
        if(!actor.IsOwner)return;
        if(data.IsAiming==ifAim)return;

        data.IsAiming=ifAim;
    }

    public bool TrySubmitBodyTurn(
        float ignoreTurnAngle,
        float maxTurnAngle)
    {
        if(!actor.IsServer||!actor.simulation.aimData.IsAiming)return false;

        float ignoreAngle=Mathf.Clamp(Mathf.Abs(ignoreTurnAngle),0f,180f);
        float maxDelta=Mathf.Clamp(Mathf.Abs(maxTurnAngle),0f,180f);
        if(maxDelta<=Mathf.Epsilon)return false;

        float desiredYaw=actor.simulation.cameraData.ViewYaw;
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

    private readonly RaycastHit[] aimHitBuffer=new RaycastHit[32];
    /// <summary>
    ///更新服务器权威target
    /// </summary>
    public void ServerTick()
    {
        if(!actor.IsServer)return;
        //服务器计算target
        if(TryResolveTarget(
           in actor.simulation.cameraData,
           out Vector3 targetPosition))
        {
            actor.simulation.aimData.TargetPosition=targetPosition;
            return;
        }

        float distance=actor.actorSO.aimSO?.TargetDistance??1f;
        //更新到simulation里，再由channel同步
        actor.simulation.aimData.TargetPosition=
            actor.transform.position+actor.transform.forward*distance;
    }

    /// <summary>
    /// 根据摄像机算目标（只允许owner设置）
    /// </summary>
    private void UpdateLocalTarget()
    {
        if(TryResolveTarget(in actor.cameraSystem.data,out Vector3 targetPosition))
            data.TargetPosition=targetPosition;
    }

    /// <summary>
    /// 尝试获取target
    /// </summary>
    /// <param name="cameraData"></param>
    /// <param name="targetPosition"></param>
    /// <returns></returns>
    private bool TryResolveTarget(
        in ActorCameraData cameraData,
        out Vector3 targetPosition)
    {
        targetPosition=default;
        AimSO config=actor.actorSO.aimSO;
        if(config==null||!IsFinite(cameraData.ViewOrigin)||
           !IsFinite(cameraData.ViewDirection)||
           cameraData.ViewDirection.sqrMagnitude<=0.000001f)return false;

        Vector3 direction=cameraData.ViewDirection.normalized;
        bool hasHit=ActorRaycastUtility.TryRaycastIgnoringActor(
            cameraData.ViewOrigin,
            direction,
            config.TargetDistance,
            config.TargetCollisionMask,
            QueryTriggerInteraction.Collide,
            actor,
            aimHitBuffer,
            out RaycastHit hit);

        targetPosition=hasHit
            ?hit.point
            :cameraData.ViewOrigin+direction*config.TargetDistance;
        return true;
    }

    /// <summary>
    /// 数据是否有效
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&
               !float.IsNaN(value.z)&&!float.IsInfinity(value.z);
    }
    /// <summary>
    /// 更新瞄准状态与target，只允许owner算自己的target，其他客户端用权威板子算的target
    /// </summary>
    public void PresentationUpdate()
    {
        if(!actor.IsOwner)
            data=actor.simulation.aimData;

        if(actor.IsOwner)
            UpdateLocalTarget();//只有owner能自己算target
    }
    public bool isDisposed;
    public void Dispose()
    {
        if(isDisposed)return;
        isDisposed=true;
        replication.Dispose();
    }

}
