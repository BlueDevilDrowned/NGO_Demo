using UnityEngine;

/// <summary>
/// 创建一颗服务器子弹时传入的固定参数。
/// </summary>
public struct ProjectileSpawnData
{
    /// <summary>服务器生成该子弹时的服务器Tick。</summary>
    public uint ShotTick;

    /// <summary>当前武器两次射击之间的Tick数，用于同步射击节奏。</summary>
    public uint FireIntervalTicks;

    /// <summary>生成该子弹的武器配置ID。</summary>
    public ushort WeaponId;

    /// <summary>子弹命中目标时使用的基础伤害。</summary>
    public float Damage;

    /// <summary>子弹的初始移动速度，单位为Unity单位每秒。</summary>
    public float Speed;

    /// <summary>子弹受到的重力加速度大小，单位为Unity单位每二次方秒。</summary>
    public float Gravity;

    /// <summary>子弹允许飞行的最大总距离，单位为Unity单位。</summary>
    public float Range;

    /// <summary>参与子弹碰撞检测的Unity LayerMask值。</summary>
    public int HitMask;

    /// <summary>子弹生成时的世界坐标。</summary>
    public Vector3 Origin;

    /// <summary>子弹生成时的世界空间方向，系统内部会将其归一化。</summary>
    public Vector3 Direction;
}

/// <summary>
/// 一颗活跃子弹在服务器上的运行时状态。
/// </summary>
internal struct ProjectileData
{
    /// <summary>当前WeaponSystem内唯一的子弹运行时ID。</summary>
    public uint Id;

    /// <summary>服务器生成该子弹时的Tick。</summary>
    public uint ShotTick;

    /// <summary>发射该子弹时武器的射击间隔Tick数。</summary>
    public uint FireIntervalTicks;

    /// <summary>生成该子弹的武器配置ID。</summary>
    public ushort WeaponId;

    /// <summary>未经命中部位倍率修正的基础伤害。</summary>
    public float Damage;

    /// <summary>子弹生成时的初始速度大小，用于构造同步事件。</summary>
    public float Speed;

    /// <summary>子弹受到的重力加速度大小。</summary>
    public float Gravity;

    /// <summary>子弹允许飞行的最大总距离。</summary>
    public float Range;

    /// <summary>参与子弹碰撞检测的Unity LayerMask值。</summary>
    public int HitMask;

    /// <summary>子弹生成时的世界坐标，生成后保持不变。</summary>
    public Vector3 Origin;

    /// <summary>子弹当前Tick的世界坐标。</summary>
    public Vector3 Position;

    /// <summary>子弹当前的世界空间速度，包含方向和速度大小。</summary>
    public Vector3 Velocity;

    /// <summary>子弹从Origin开始累计飞行的距离。</summary>
    public float TravelledDistance;
}
