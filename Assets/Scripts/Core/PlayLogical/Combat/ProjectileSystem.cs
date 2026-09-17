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

    public uint Spawn(in ProjectileSpawnData spawnData,uint currentServerTick)
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
            LastSimulatedTick=spawnData.ShotTick,
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
        LagCompensationWorld.System?.BeginDebugShot(
            owner,
            projectileId,
            spawnData.ShotTick,
            currentServerTick,
            spawnData.Origin);
        PublishEvent(
            in projectile,
            ShotEventType.Spawn,
            spawnData.ShotTick,
            spawnData.Origin+direction);

        while(projectile.LastSimulatedTick!=currentServerTick)
        {
            uint simulationTick=projectile.LastSimulatedTick+1;
            if(SimulateStep(
                   ref projectile,
                   simulationTick,
                   currentServerTick,
                   TickTime.deltaTime,
                   true))
                return projectileId;
        }

        activeProjectiles.Add(projectile);
        return projectileId;
    }

    public void ServerTick(uint currentServerTick,float deltaTime)
    {
        if(activeProjectiles.Count==0)return;

        //更新活跃的子弹
        for(int i=activeProjectiles.Count-1;i>=0;i--)
        {
            ProjectileData projectile=activeProjectiles[i];
            if(TickDifference(currentServerTick,projectile.LastSimulatedTick)<=0)
                continue;

            if(SimulateStep(
                   ref projectile,
                   currentServerTick,
                   currentServerTick,
                   deltaTime,
                   false))
            {
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

    private ProjectileHitResult ResolveHit(
        in ProjectileData projectile,
        in RaycastHit hit)
    {
        ProjectileHitContext context=new(
            owner,
            projectile.Id,
            projectile.Damage,
            projectile.Velocity,
            in hit);
        return hitResolver.Resolve(in context);
    }

    private ProjectileHitResult ResolveHit(
        in ProjectileData projectile,
        in LagCompensatedHit hit)
    {
        ProjectileHitContext context=new(
            owner,
            projectile.Id,
            projectile.Damage,
            projectile.Velocity,
            in hit);
        return hitResolver.Resolve(in context);
    }

    private bool SimulateStep(
        ref ProjectileData projectile,
        uint simulationTick,
        uint resolveServerTick,
        float deltaTime,
        bool rewind)
    {
        Vector3 gravityDirection=Physics.gravity.sqrMagnitude>0.000001f
            ?Physics.gravity.normalized
            :Vector3.down;
        Vector3 acceleration=gravityDirection*projectile.Gravity;
        Vector3 nextPosition=projectile.Position+
            projectile.Velocity*deltaTime+
            0.5f*acceleration*deltaTime*deltaTime;
        Vector3 segment=nextPosition-projectile.Position;
        float segmentDistance=segment.magnitude;
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

        Vector3 direction=segmentDistance>0.000001f
            ?segment/segmentDistance
            :Vector3.zero;
        if(segmentDistance>0.000001f)
        {
            if(rewind&&TryResolveRewindHit(
                   in projectile,
                   simulationTick,
                   direction,
                   segmentDistance,
                   out LagCompensatedHit rewindHit))
            {
                LagCompensationWorld.System?.RecordDebugSegment(
                    owner,
                    projectile.Id,
                    simulationTick,
                    projectile.Position,
                    rewindHit.Point,
                    true);
                ProjectileHitResult result=ResolveHit(in projectile,in rewindHit);
                LagCompensationWorld.System?.CompleteDebugHit(
                    owner,
                    projectile.Id,
                    simulationTick,
                    resolveServerTick,
                    rewindHit.Body,
                    rewindHit.SourceCollider,
                    rewindHit.Point,
                    rewindHit.Normal,
                    in result);
                PublishEvent(
                    in projectile,
                    ShotEventType.Hit,
                    simulationTick,
                    rewindHit.Point,
                    rewindHit.Normal,
                    rewindHit.SourceCollider!=null
                        ?rewindHit.SourceCollider.gameObject.layer
                        :-1);
                projectile.LastSimulatedTick=simulationTick;
                return true;
            }

            if(!rewind&&TryResolveHit(
                   in projectile,
                   direction,
                   segmentDistance,
                   out RaycastHit hit))
            {
                LagCompensationWorld.System?.RecordDebugSegment(
                    owner,
                    projectile.Id,
                    simulationTick,
                    projectile.Position,
                    hit.point,
                    true);
                ProjectileHitResult result=ResolveHit(in projectile,in hit);
                LagCompensatedBody targetBody=result.Target!=null
                    ?result.Target.lagCompensatedBody
                    :hit.collider.GetComponentInParent<LagCompensatedBody>();
                LagCompensationWorld.System?.CompleteDebugHit(
                    owner,
                    projectile.Id,
                    simulationTick,
                    resolveServerTick,
                    targetBody,
                    hit.collider,
                    hit.point,
                    hit.normal,
                    in result);
                PublishEvent(
                    in projectile,
                    ShotEventType.Hit,
                    simulationTick,
                    hit.point,
                    hit.normal,
                    hit.collider.gameObject.layer);
                projectile.LastSimulatedTick=simulationTick;
                return true;
            }
        }

        projectile.Position=nextPosition;
        projectile.Velocity+=acceleration*deltaTime;
        projectile.TravelledDistance+=segmentDistance;
        projectile.LastSimulatedTick=simulationTick;
        LagCompensationWorld.System?.RecordDebugSegment(
            owner,
            projectile.Id,
            simulationTick,
            nextPosition-segment,
            nextPosition,
            false);

        if(!reachesRange&&remainingDistance>0.000001f)
            return false;

        PublishEvent(
            in projectile,
            ShotEventType.Expired,
            simulationTick,
            nextPosition);
        LagCompensationWorld.System?.CompleteDebugExpired(
            owner,
            projectile.Id,
            simulationTick,
            resolveServerTick,
            nextPosition);
        return true;
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

    private bool TryResolveRewindHit(
        in ProjectileData projectile,
        uint rewindTick,
        Vector3 direction,
        float distance,
        out LagCompensatedHit hit)
    {
        hit=default;
        LagCompensationSystem system=LagCompensationWorld.System;
        return system!=null&&system.Raycast(
            rewindTick,
            projectile.Position,
            direction,
            distance,
            projectile.HitMask,
            owner.lagCompensatedBody,
            out hit);
    }

    private static int TickDifference(uint current,uint previous)
    {
        return unchecked((int)(current-previous));
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
