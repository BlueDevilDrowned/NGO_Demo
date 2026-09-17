using UnityEngine;

public interface IProjectileHitReceiver
{
    void ReceiveProjectileHit(in ProjectileHitResult hit);
}

public readonly struct ProjectileHitContext
{
    public Actor Shooter{get;}
    public uint ProjectileId{get;}
    public float BaseDamage{get;}
    public Vector3 Direction{get;}
    public Collider Collider{get;}
    public Hitbox Hitbox{get;}
    public Vector3 Point{get;}
    public Vector3 Normal{get;}

    public ProjectileHitContext(
        Actor shooter,
        uint projectileId,
        float baseDamage,
        Vector3 direction,
        in RaycastHit physicsHit)
    {
        Shooter=shooter;
        ProjectileId=projectileId;
        BaseDamage=baseDamage;
        Direction=direction.sqrMagnitude>0.000001f
            ?direction.normalized
            :Vector3.zero;
        Collider=physicsHit.collider;
        Hitbox=physicsHit.collider!=null
            ?physicsHit.collider.GetComponent<Hitbox>()
            :null;
        Point=physicsHit.point;
        Normal=physicsHit.normal;
    }

    public ProjectileHitContext(
        Actor shooter,
        uint projectileId,
        float baseDamage,
        Vector3 direction,
        in LagCompensatedHit rewindHit)
    {
        Shooter=shooter;
        ProjectileId=projectileId;
        BaseDamage=baseDamage;
        Direction=direction.sqrMagnitude>0.000001f
            ?direction.normalized
            :Vector3.zero;
        Collider=rewindHit.SourceCollider;
        Hitbox=rewindHit.Hitbox;
        Point=rewindHit.Point;
        Normal=rewindHit.Normal;
    }
}

public readonly struct ProjectileHitResult
{
    public Actor Shooter{get;}
    public Actor Target{get;}
    public Hitbox Hitbox{get;}
    public uint ProjectileId{get;}
    public HitLocation Location{get;}
    public float Damage{get;}
    public Vector3 Point{get;}
    public Vector3 Normal{get;}
    public Vector3 Direction{get;}

    public bool HasActorTarget=>Target!=null;

    public ProjectileHitResult(
        Actor shooter,
        Actor target,
        Hitbox hitbox,
        uint projectileId,
        HitLocation location,
        float damage,
        Vector3 point,
        Vector3 normal,
        Vector3 direction)
    {
        Shooter=shooter;
        Target=target;
        Hitbox=hitbox;
        ProjectileId=projectileId;
        Location=location;
        Damage=damage;
        Point=point;
        Normal=normal;
        Direction=direction;
    }
}

/// <summary>
/// 封闭类，用于解析和处理投射物命中结果
/// </summary>
public sealed class ProjectileHitResolver
{
    /// <summary>
    /// 解析投射物命中上下文并返回命中结果
    /// </summary>
    /// <param name="context">投射物命中上下文，包含命中相关的所有信息</param>
    /// <returns>返回包含命中详细信息的ProjectileHitResult对象</returns>
    public ProjectileHitResult Resolve(in ProjectileHitContext context)
    {
        // 初始化命中框、目标、命中位置和伤害值
        Hitbox hitbox=context.Hitbox;
        Actor target=null;
        HitLocation location=HitLocation.Unknown;
        float damage=Mathf.Max(0f,context.BaseDamage);

        // 检查是否有碰撞器以及是否包含命中框组件
        if(hitbox==null&&context.Collider!=null)
            context.Collider.TryGetComponent(out hitbox);

        if(hitbox!=null)
        {
            // 获取命中框所有者作为目标
            target=hitbox.Manager!=null?hitbox.Manager.Owner:null;
            // 获取命中位置
            location=hitbox.Location;
            // 根据命中框的伤害倍率调整伤害值
            damage*=Mathf.Max(0f,hitbox.DamageMultiplier);
        }

        // 防止射击者成为自己的目标
        if(target==context.Shooter)
            target=null;

        // 创建命中结果对象
        ProjectileHitResult result=new(
            context.Shooter,
            target,
            hitbox,
            context.ProjectileId,
            location,
            damage,
            context.Point,
            context.Normal,
            context.Direction);

        // 所有命中后逻辑仍从 Actor 组合入口分发。
        if(target!=null)
            target.ReceiveProjectileHit(in result);

        // 返回命中结果
        return result;
    }
}
