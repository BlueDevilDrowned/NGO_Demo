using System;
using System.Collections.Generic;
using UnityEngine;

public interface IProjectileEventSink
{
    void PublishProjectileEvent(in ShotData projectileEvent);
}

/// <summary>
/// 模拟一个WeaponSystem发射的所有服务器子弹。
/// </summary>
public sealed class ProjectileSystem
{
    private readonly Actor owner;
    /// <summary>
    /// 命中事件管理接口
    /// </summary>
    private readonly IProjectileEventSink eventSink;
    private readonly List<ProjectileData> activeProjectiles=new();
    private readonly RaycastHit[] raycastHits=new RaycastHit[64];
    private readonly ProjectileHitResolver hitResolver=new();
    private uint projectileSequence;

    public int ActiveCount=>activeProjectiles.Count;

    public ProjectileSystem(Actor owner,IProjectileEventSink eventSink)
    {
        this.owner=owner??throw new ArgumentNullException(nameof(owner));
        this.eventSink=eventSink??
            throw new ArgumentNullException(nameof(eventSink));
    }

    public uint Spawn(in ProjectileSpawnData spawnData)
    {
        if(!IsValid(in spawnData))
        {
            Debug.LogError("Projectile spawn data is invalid.");
            return 0;
        }

        uint projectileId=++projectileSequence;
        Vector3 direction=spawnData.Direction.normalized;
        ProjectileData projectile=new()
        {
            Id=projectileId,
            ShotTick=spawnData.ShotTick,
            FireIntervalTicks=spawnData.FireIntervalTicks,
            WeaponId=spawnData.WeaponId,
            Damage=spawnData.Damage,
            Speed=spawnData.Speed,
            Gravity=spawnData.Gravity,
            Range=spawnData.Range,
            HitMask=spawnData.HitMask,
            Origin=spawnData.Origin,
            Position=spawnData.Origin,
            Velocity=direction*spawnData.Speed,
        };
        activeProjectiles.Add(projectile);

        PublishEvent(
            in projectile,
            ShotEventType.Spawn,
            spawnData.ShotTick,
            spawnData.Origin+direction);
        return projectileId;
    }

    public void ServerTick(uint currentServerTick,float deltaTime)
    {
        if(activeProjectiles.Count==0)return;

        Vector3 gravityDirection=Physics.gravity.sqrMagnitude>0.000001f
            ?Physics.gravity.normalized
            :Vector3.down;
        //更新活跃的子弹
        for(int i=activeProjectiles.Count-1;i>=0;i--)
        {
            //只更新tick<当前服务器tick的子弹
            ProjectileData projectile=activeProjectiles[i];
            if(currentServerTick<=projectile.ShotTick)continue;
            //积分竖直位移
            Vector3 acceleration=gravityDirection*projectile.Gravity;
            Vector3 nextPosition=projectile.Position+
                projectile.Velocity*deltaTime+
                0.5f*acceleration*deltaTime*deltaTime;
            //变化向量
            Vector3 segment=nextPosition-projectile.Position;
            //变化距离
            float segmentDistance=segment.magnitude;

            ///超出距离的子弹只允许到达最远距离
            float remainingDistance=Mathf.Max(
                0f,
                projectile.Range-projectile.TravelledDistance);

            bool reachesRange=segmentDistance>=remainingDistance;
            if(reachesRange&&segmentDistance>0.000001f)
            {
                segment=segment/segmentDistance*remainingDistance;
                segmentDistance=remainingDistance;
                nextPosition=projectile.Position+segment;
            }
            ///
            //有变化时尝试获取击中物体
            if(segmentDistance>0.000001f&&
               TryResolveHit(
                   in projectile,
                   segment/segmentDistance,
                   segmentDistance,
                   out RaycastHit hit))
            {
                //有命中则转换成命中信息，并发布事件
                ResolveHit(in projectile,in hit);
                PublishEvent(
                    in projectile,
                    ShotEventType.Hit,
                    currentServerTick,
                    hit.point,
                    hit.normal,
                    hit.collider.gameObject.layer);
                activeProjectiles.RemoveAt(i);
                continue;
            }

            //超出距离则发布结束事件
            projectile.Position=nextPosition;
            projectile.Velocity+=acceleration*deltaTime;
            projectile.TravelledDistance+=segmentDistance;

            if(reachesRange||remainingDistance<=0.000001f)
            {
                PublishEvent(
                    in projectile,
                    ShotEventType.Expired,
                    currentServerTick,
                    nextPosition);
                activeProjectiles.RemoveAt(i);
                continue;
            }

            activeProjectiles[i]=projectile;
        }
    }

    public void Clear()
    {
        activeProjectiles.Clear();
        projectileSequence=0;
    }

    private void ResolveHit(
        in ProjectileData projectile,
        in RaycastHit hit)
    {
        ProjectileHitContext context=new(
            owner,
            projectile.Id,
            projectile.Damage,
            projectile.Velocity,
            in hit);
        hitResolver.Resolve(in context);
    }


    /// <summary>
    /// 产生获取命中
    /// </summary>
    /// <param name="projectile"></param>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <param name="closestHit"></param>
    /// <returns></returns>
    private bool TryResolveHit(
        in ProjectileData projectile,
        Vector3 direction,
        float distance,
        out RaycastHit closestHit)
    {
        return ActorRaycastUtility.TryRaycastIgnoringActor(
            projectile.Position,
            direction,
            distance,
            projectile.HitMask,
            QueryTriggerInteraction.Collide,
            owner,
            raycastHits,
            out closestHit);
    }

    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="projectile"></param>
    /// <param name="eventType"></param>
    /// <param name="eventTick"></param>
    /// <param name="endPoint"></param>
    /// <param name="hitNormal"></param>
    /// <param name="hitLayer"></param>
    private void PublishEvent(
        in ProjectileData projectile,
        ShotEventType eventType,
        uint eventTick,
        Vector3 endPoint,
        Vector3 hitNormal=default,
        int hitLayer=-1)
    {
        bool hasHit=eventType==ShotEventType.Hit;
        ShotData projectileEvent=new()
        {
            ProjectileId=projectile.Id,
            ShotTick=projectile.ShotTick,
            EventTick=eventTick,
            FireIntervalTicks=projectile.FireIntervalTicks,
            WeaponId=projectile.WeaponId,
            EventType=eventType,
            TracerSpeed=projectile.Speed,
            Gravity=projectile.Gravity,
            Range=projectile.Range,
            Origin=projectile.Origin,
            EndPoint=endPoint,
            HasHit=hasHit,
            HitLayer=hasHit&&hitLayer>=0&&hitLayer<=31
                ?(byte)hitLayer
                :byte.MaxValue,
            HitNormal=hitNormal,
        };
        eventSink.PublishProjectileEvent(in projectileEvent);
    }

    private static bool IsValid(in ProjectileSpawnData spawnData)
    {
        return spawnData.WeaponId>0&&
               spawnData.Speed>0f&&
               spawnData.Gravity>=0f&&
               spawnData.Range>0f&&
               spawnData.Direction.sqrMagnitude>0.000001f;
    }
}
