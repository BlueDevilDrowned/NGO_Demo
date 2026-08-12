using System.Collections.Generic;
using UnityEngine;

public interface IProjectileEventSink
{
    void PublishProjectileEvent(in ShotData projectileEvent);
}

public struct ProjectileSpawnData
{
    public Actor Owner;
    public IProjectileEventSink EventSink;
    public uint ShotTick;
    public uint FireIntervalTicks;
    public ushort WeaponId;
    public WeaponType WeaponType;
    public float Damage;
    public float Speed;
    public float Gravity;
    public float Range;
    public int HitMask;
    public Vector3 Origin;
    public Vector3 Direction;
}

/// <summary>
/// 封装了投射物系统的核心逻辑，包括投射物的创建、更新、命中检测和事件处理
/// </summary>
public sealed class ProjectileSystem
{
    /// <summary>
    /// 表示一个活跃的投射物，包含其所有必要的状态信息
    /// </summary>
    private struct ActiveProjectile
    {
        public ushort WeaponId;
        public uint Id; // 投射物唯一标识符
        public Actor Owner; // 拥有该投射物的角色
        public IProjectileEventSink EventSink; // 投射物事件接收器
        public uint ShotTick; // 发射时的时间刻
        public uint FireIntervalTicks; // 发射间隔时间刻
        public WeaponType WeaponType; // 武器类型
        public float Damage; // 基础伤害
        public float Speed; // 投射物速度
        public float Gravity; // 重力影响
        public float Range; // 最大射程
        public int HitMask; // 命撞检测层
        public Vector3 Origin; // 起始位置
        public Vector3 Position; // 当前位置
        public Vector3 Velocity; // 当前速度
        public float TravelledDistance; // 已行进距离
    }

    /// <summary>
    /// 获取全局共享的投射物系统实例
    /// </summary>
    public static ProjectileSystem Shared{get;}=new();

    // 存储所有活跃的投射物
    private readonly List<ActiveProjectile> activeProjectiles=new();
    // 用于射线检测的缓存数组
    private readonly RaycastHit[] raycastHits=new RaycastHit[64];
    private readonly ProjectileHitResolver hitResolver=new();
    // 投射物序列号生成器
    private uint projectileSequence;

    /// <summary>
    /// 获取当前活跃的投射物数量
    /// </summary>
    public int ActiveCount=>activeProjectiles.Count;

    /// <summary>
    /// 私有构造函数，确保单例模式
    /// </summary>
    private ProjectileSystem()
    {
    }

    /// <summary>
    /// 生成一个新的投射物
    /// </summary>
    /// <param name="spawnData">投射物生成数据</param>
    /// <returns>生成的投射物ID，如果生成失败则返回0</returns>
    public uint Spawn(in ProjectileSpawnData spawnData)
    {
        // 验证投射物生成数据的有效性
        if(spawnData.Owner==null||spawnData.EventSink==null||
           spawnData.Speed<=0f||spawnData.Gravity<0f||
           spawnData.Range<=0f||
           spawnData.Direction.sqrMagnitude<=0.000001f)
        {
            Debug.LogError("Projectile spawn data is invalid.");
            return 0;
        }

        // 生成新的投射物序列号
        projectileSequence++;
        // 标准化方向向量
        Vector3 direction=spawnData.Direction.normalized;
        // 创建新的投射物实例
        ActiveProjectile projectile=new ActiveProjectile
        {
            Id=projectileSequence,
            Owner=spawnData.Owner,
            EventSink=spawnData.EventSink,
            ShotTick=spawnData.ShotTick,
            FireIntervalTicks=spawnData.FireIntervalTicks,
            WeaponId=spawnData.WeaponId,
            WeaponType=spawnData.WeaponType,
            Damage=spawnData.Damage,
            Speed=spawnData.Speed,
            Gravity=spawnData.Gravity,
            Range=spawnData.Range,
            HitMask=spawnData.HitMask,
            Origin=spawnData.Origin,
            Position=spawnData.Origin,
            Velocity=direction*spawnData.Speed,
        };
        // 将新投射物添加到活跃列表
        activeProjectiles.Add(projectile);

        // 创建并发布生成事件
        ShotData spawnEvent=CreateEvent(
            in projectile,
            ShotEventType.Spawn,
            spawnData.ShotTick,
            spawnData.Origin+direction,
            false,
            Vector3.zero);
        spawnData.EventSink.PublishProjectileEvent(in spawnEvent);
        return projectile.Id;
    }

    /// <summary>
    /// 服务器端更新投射物状态
    /// </summary>
    /// <param name="currentServerTick">当前服务器时间刻</param>
    /// <param name="deltaTime">帧间隔时间</param>
    public void ServerTick(uint currentServerTick,float deltaTime)
    {
        // 如果没有活跃的投射物，直接返回
        if(activeProjectiles.Count==0)return;

        // 获取重力方向
        Vector3 gravityDirection=Physics.gravity.sqrMagnitude>0.000001f
            ?Physics.gravity.normalized
            :Vector3.down;

        // 从后向前遍历活跃投射物列表，以便安全地删除元素
        for(int i=activeProjectiles.Count-1;i>=0;i--)
        {
            ActiveProjectile projectile=activeProjectiles[i];
            // 检查投射物所有者和事件接收器是否有效
            if(projectile.Owner==null||projectile.EventSink==null)
            {
                activeProjectiles.RemoveAt(i);
                continue;
            }
            // 如果当前时间早于发射时间，跳过该投射物
            if(currentServerTick<=projectile.ShotTick)continue;

            // 计算重力加速度
            Vector3 acceleration=gravityDirection*projectile.Gravity;
            // 计算下一位置
            Vector3 nextPosition=projectile.Position+
                projectile.Velocity*deltaTime+
                0.5f*acceleration*deltaTime*deltaTime;
            // 计算位移向量
            Vector3 segment=nextPosition-projectile.Position;
            float segmentDistance=segment.magnitude;
            // 计算剩余可行进距离
            float remainingDistance=
                Mathf.Max(0f,projectile.Range-projectile.TravelledDistance);
            // 检查是否达到最大射程
            bool reachesRange=segmentDistance>=remainingDistance;
            if(reachesRange&&segmentDistance>0.000001f)
            {
                // 调整位移向量使其不超过剩余距离
                segment=segment/segmentDistance*remainingDistance;
                segmentDistance=remainingDistance;
                nextPosition=projectile.Position+segment;
            }

            // 检测碰撞
            if(segmentDistance>0.000001f&&
               TryResolveHit(
                   in projectile,
                   segment/segmentDistance,
                   segmentDistance,
                   out RaycastHit hit))
            {
                ProjectileHitContext hitContext=new(
                    projectile.Owner,
                    projectile.Id,
                    projectile.WeaponType,
                    projectile.Damage,
                    projectile.Velocity,
                    in hit);
                hitResolver.Resolve(in hitContext);

                // 创建并发布命中事件
                ShotData hitEvent=CreateEvent(
                    in projectile,
                    ShotEventType.Hit,
                    currentServerTick,
                    hit.point,
                    true,
                    hit.normal,
                    hit.collider.gameObject.layer);
                projectile.EventSink.PublishProjectileEvent(in hitEvent);
                activeProjectiles.RemoveAt(i);
                continue;
            }

            // 更新投射物位置和速度
            projectile.Position=nextPosition;
            projectile.Velocity+=acceleration*deltaTime;
            projectile.TravelledDistance+=segmentDistance;
            // 检查是否达到最大射程或超出射程
            if(reachesRange||remainingDistance<=0.000001f)
            {
                // 创建并发布过期事件
                ShotData expiredEvent=CreateEvent(
                    in projectile,
                    ShotEventType.Expired,
                    currentServerTick,
                    nextPosition,
                    false,
                    Vector3.zero,
                    -1);
                projectile.EventSink.PublishProjectileEvent(in expiredEvent);
                activeProjectiles.RemoveAt(i);
                continue;
            }

            // 更新投射物列表中的数据
            activeProjectiles[i]=projectile;
        }
    }

    /// <summary>
    /// 根据所有者取消其所有的投射物
    /// </summary>
    /// <param name="owner">角色所有者</param>
    public void CancelByOwner(Actor owner)
    {
        if(owner==null)return;

        // 从后向前遍历并删除属于指定所有者的投射物
        for(int i=activeProjectiles.Count-1;i>=0;i--)
        {
            if(activeProjectiles[i].Owner==owner)
                activeProjectiles.RemoveAt(i);
        }
    }

    /// <summary>
    /// 清除所有活跃的投射物
    /// </summary>
    public void Clear()
    {
        activeProjectiles.Clear();
        projectileSequence=0;
    }

    private bool TryResolveHit(
        in ActiveProjectile projectile,
        Vector3 direction,
        float distance,
        out RaycastHit closestHit)
    {
        int hitCount=Physics.RaycastNonAlloc(
            projectile.Position,
            direction,
            raycastHits,
            distance,
            projectile.HitMask,
            QueryTriggerInteraction.Collide);
        closestHit=default;
        float closestDistance=float.PositiveInfinity;

        for(int i=0;i<hitCount;i++)
        {
            RaycastHit candidate=raycastHits[i];
            Transform hitTransform=candidate.collider.transform;
            if(hitTransform==projectile.Owner.transform||
               hitTransform.IsChildOf(projectile.Owner.transform))continue;
            if(candidate.collider.TryGetComponent(out Hitbox hitbox)&&
               hitbox.Manager!=null&&
               hitbox.Manager.Owner==projectile.Owner)continue;
            if(candidate.distance>=closestDistance)continue;

            closestDistance=candidate.distance;
            closestHit=candidate;
        }

        return !float.IsPositiveInfinity(closestDistance);
    }

    private static ShotData CreateEvent(
        in ActiveProjectile projectile,
        ShotEventType eventType,
        uint eventTick,
        Vector3 endPoint,
        bool hasHit,
        Vector3 hitNormal,
        int hitLayer=-1)
    {
        return new ShotData
        {
            ProjectileId=projectile.Id,
            ShotTick=projectile.ShotTick,
            EventTick=eventTick,
            FireIntervalTicks=projectile.FireIntervalTicks,
            WeaponId=projectile.WeaponId,
            WeaponType=projectile.WeaponType,
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
    }
}
