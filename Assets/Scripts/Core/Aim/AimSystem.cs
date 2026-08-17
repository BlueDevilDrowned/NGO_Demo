using System;
using UnityEditor;
using UnityEngine;


/// <summary>
/// 主要负责表现层，目前准备各个系统同时维护表现层与权威层，表现层数据经过权威层仲裁后返回表现层
/// </summary>
public class AimSystem:IActorSystem
{
    public Actor actor;
    public AimReplication replication;
    public AimSystem(Actor actor)
    {
        this.actor=actor;
        OnAimChange+=OnAimChanged;
        PreAimState=false;
        
        actor.RegisterSystem(this);
    }

    /// <summary>
    /// 表现层数据，客户端维护，服务器仲裁
    /// </summary>
    public AimData data;
    public bool IsAiming=>data.IsAiming;
    public Action OnAimChange;
    /// <summary>
    /// 客户端修改aim再由服务器决定是否接收，纠正状态
    /// </summary>
    /// <param name="ifAim"></param>
    public void SetPresentationAim(bool ifAim)
    {
        //只允许本机预测
        if(!actor.IsOwner)return;
        data.IsAiming=ifAim;
        //计算表现层target
        AimTargetUpdate();

    }
    //避免每帧引用导致GC
    private readonly RaycastHit[] aimHitBuffer=new RaycastHit[32];
    private void AimTargetUpdate()
    {
        //忽略自身
        ActorRaycastUtility.TryRaycastIgnoringActor(
            actor.cameraSystem.data.ViewOrigin,
            actor.cameraSystem.data.ViewDirection,
            actor.actorSO.aimSO.TargetDistance,
            actor.actorSO.aimSO.TargetCollisionMask,
            QueryTriggerInteraction.Collide,
            actor,
            aimHitBuffer,
            out RaycastHit hit);
        actor.aimRig.SetTargetPosition(hit.transform.position);
    }
    /// <summary>
    /// 有两种可能，客户端自己切换aim，服务器同步权威数据导致切换
    /// </summary>
    public void OnAimChanged()
    {
        if(data.IsAiming)
        {
            //瞄准状态
            //状态机切换瞄准状态机，摄像机切换瞄准模式

            //摄像机部分已由摄像机维护
        }
    }
    private bool PreAimState=false;
    /// <summary>
    /// 更新瞄准状态与target
    /// </summary>
    public void PresentationUpdate()
    {
        if(!actor.IsOwner)return;
        if(PreAimState!=data.IsAiming)
        {
            OnAimChange?.Invoke();
        }
    }
    public bool isDisposed;
    public void Dispose()
    {
        if(isDisposed)return;
        isDisposed=true;
        replication.Dispose();
    }

}