using System.Collections.Generic;
using UnityEngine;
// 引入命名空间以使用Queue类
/// <summary>
/// 武器系统类，实现了IProjectileEventSink接口，用于处理武器射击相关逻辑
/// </summary>
public class WeaponSystem : IProjectileEventSink
{
    // 持有武器系统的角色引用
    public Actor actor;
    private readonly WeaponEquipmentSystem equipment;

    // 下次射击的游戏刻度
    private uint nextFireTick;
    // 事件序列号
    private uint eventSequence;
    // 最后应用的事件序列号
    private uint lastAppliedEventSequence;
    // 存储最近事件的环形缓冲区
    private readonly ShotData[] recentEvents=
        new ShotData[WeaponSnapshot.MaxEvents];
    // 最近事件的起始索引
    private int recentEventStart;
    // 最近事件的数量
    private int recentEventCount;
    // 待处理的展示事件队列
    private readonly Queue<ShotData> pendingPresentationEvents=new();
    // 待处理的射击动画队列
    private readonly Queue<ShotData> pendingFireAnimations=new();
    // 武器展示系统
    private readonly Dictionary<ushort,WeaponPresentationSystem> presentations=new();

    // 射击序列属性（只读）
    public uint ShotSequence{get;private set;}
    public ushort CurrentWeaponId=>equipment?.CurrentWeaponId??0;
    // 最后一次射击数据（只读）
    public ShotData LastShot{get;private set;}

    /// <summary>
    /// 武器系统构造函数
    /// </summary>
    /// <param name="actor">持有该武器系统的角色</param>
    public WeaponSystem(Actor actor,WeaponEquipmentSystem equipment)
    {
        this.actor=actor;
        this.equipment=equipment;
        //武器改变调整ik位置
        if(this.equipment!=null)
            this.equipment.WeaponChanged+=OnWeaponChanged;
    }

    public bool TryEquip(WeaponSO definition)
    {
        return actor.IsServer&&
               equipment!=null&&
               definition!=null&&
               equipment.Equip(definition.Id);
    }

    public bool TryEquip(ushort weaponId)
    {
        if(!actor.IsServer||equipment==null)return false;

        return equipment.Equip(weaponId);
    }

    public bool TryUnequip()
    {
        if(!actor.IsServer||equipment==null)return false;

        equipment.Unequip();
        return true;
    }

    /// <summary>
    /// 尝试射击武器
    /// </summary>
    /// <returns>是否成功射击</returns>
    public bool TryFire()
    {
        // 检查是否为服务器端
        if(!actor.IsServer)return false;
        // 验证武器配置是否有效
        WeaponSO definition=equipment?.CurrentDefinition;
        Transform muzzle=equipment?.Muzzle;
        if(definition==null)return false; 
        if(definition.FireRate<=0||
           definition.Range<=0f||definition.TracerSpeed<=0f||
           definition.ProjectileGravity<0f)
        {
            Debug.LogError("Weapon configuration is invalid.");
            return false;
        }
        // 检查枪口是否已配置
        if(muzzle==null)
        {
            Debug.LogError("Weapon muzzle is not configured.");
            return false;
        }

        // 获取当前服务器刻度
        uint currentServerTick=TickTime.CurrentServerTick;
        // 检查是否达到射击间隔
        if(currentServerTick<nextFireTick)return false;

        // 计算射击起点和方向
        Vector3 origin=muzzle.position;
        Vector3 direction=ResolveFireDirection(origin);
        // 计算射击间隔刻度数
        uint fireIntervalTicks=GetFireIntervalTicks();
        // 创建射击生成数据
        ProjectileSpawnData spawnData=new ProjectileSpawnData
        {
            Owner=actor,
            EventSink=this,
            ShotTick=currentServerTick,
            FireIntervalTicks=fireIntervalTicks,
            WeaponId=definition.Id,
            WeaponType=definition.Type,
            Damage=definition.Damage,
            Speed=definition.TracerSpeed,
            Gravity=definition.ProjectileGravity,
            Range=definition.Range,
            HitMask=definition.HitMask,
            Origin=origin,
            Direction=direction,
        };
        // 生成投射物
        uint projectileId=ProjectileSystem.Shared.Spawn(in spawnData);
        if(projectileId==0)return false;

        // 更新射击序列和下次射击刻度
        ShotSequence=projectileId;
        nextFireTick=currentServerTick+fireIntervalTicks;
        return true;
    }

    /// <summary>
    /// 发布投射物事件
    /// </summary>
    /// <param name="projectileEvent">射击事件数据</param>
    public void PublishProjectileEvent(in ShotData projectileEvent)
    {
        // 检查是否为服务器端
        if(!actor.IsServer)return;

        // 创建权威事件
        ShotData authoritativeEvent=projectileEvent;
        eventSequence++;
        authoritativeEvent.Sequence=eventSequence;
        LastShot=authoritativeEvent;
        // 添加到最近事件
        AddRecentEvent(in authoritativeEvent);
        // 如果是客户端，加入展示队列
        if(actor.IsClient)
            QueuePresentationEvent(in authoritativeEvent);
    }

    /// <summary>
    /// 应用权威射击事件
    /// </summary>
    /// <param name="authoritativeEvent">权威射击事件数据</param>
    public void ApplyAuthoritativeShot(in ShotData authoritativeEvent)
    {
        // 检查事件序列是否有效
        if(authoritativeEvent.Sequence==0||
           authoritativeEvent.Sequence<=lastAppliedEventSequence)return;

        // 更新最后应用的事件序列
        lastAppliedEventSequence=authoritativeEvent.Sequence;
        LastShot=authoritativeEvent;
        // 加入展示队列
        QueuePresentationEvent(in authoritativeEvent);
    }

    /// <summary>
    /// 复制最近事件到快照
    /// </summary>
    /// <param name="snapshot">武器快照引用</param>
    public void CopyRecentEvents(ref WeaponSnapshot snapshot)
    {
        // 设置事件数量
        snapshot.EventCount=(byte)recentEventCount;
        // 复制所有最近事件
        for(int i=0;i<recentEventCount;i++)
        {
            int index=(recentEventStart+i)%recentEvents.Length;
            ShotData shotEvent=recentEvents[index];
            snapshot.SetEvent(i,in shotEvent);
        }
    }

    /// <summary>
    /// 展示更新，处理待展示的射击事件
    /// </summary>
    public void PresentationUpdate()
    {
        // 处理所有待展示的射击事件
        while(pendingPresentationEvents.Count>0)
        {
            ShotData shotEvent=pendingPresentationEvents.Dequeue();
            WeaponPresentationSystem presentation=
                GetOrCreatePresentation(shotEvent.WeaponId);
            presentation?.Apply(in shotEvent);
        }
    }

    /// <summary>
    /// 尝试消耗射击展示
    /// </summary>
    /// <param name="shot">输出的射击数据</param>
    /// <returns>是否有可消耗的射击展示</returns>
    public bool TryConsumeShotPresentation(out ShotData shot)
    {
        shot=default;
        // 检查是否有待处理的射击动画
        if(pendingFireAnimations.Count==0)return false;

        // 获取并移除队列中的第一个射击动画
        shot=pendingFireAnimations.Dequeue();
        return true;
    }

    /// <summary>
    /// 初始化展示系统
    /// </summary>
    public void InitializePresentation()
    {
        // 清理现有展示系统
        // 如果有武器配置，创建新的展示系统
        if(equipment?.CurrentDefinition!=null)
            GetOrCreatePresentation(equipment.CurrentDefinition.Id);
    }

    /// <summary>
    /// 清理展示系统
    /// </summary>
    public void DisposePresentation()
    {
        // 释放展示系统资源
        foreach(WeaponPresentationSystem presentation in presentations.Values)
            presentation.Dispose();
        presentations.Clear();
        // 清空展示队列
        pendingPresentationEvents.Clear();
        pendingFireAnimations.Clear();
    }

    public void Dispose()
    {
        if(equipment!=null)
            equipment.WeaponChanged-=OnWeaponChanged;
        DisposePresentation();
    }

    /// <summary>
    /// 获取射击间隔刻度数
    /// </summary>
    /// <returns>射击间隔的刻度数</returns>
    private uint GetFireIntervalTicks()
    {
        // 计算每发射击所需的刻度数
        WeaponSO definition=equipment?.CurrentDefinition;
        if(definition==null)return 1;

        float ticksPerShot=(float)TickTime.TickRate/definition.FireRate;
        return (uint)Mathf.Max(1,Mathf.CeilToInt(ticksPerShot));
    }

    private void OnWeaponChanged(WeaponInstance weaponInstance)
    {
        nextFireTick=0;
        if(actor.IsClient)
            InitializePresentation();
    }

    private WeaponPresentationSystem GetOrCreatePresentation(ushort weaponId)
    {
        if(weaponId==0)return null;
        if(presentations.TryGetValue(
            weaponId,
            out WeaponPresentationSystem presentation))return presentation;
        if(!WeaponCatalog.TryGet(weaponId,out WeaponSO definition))
            return null;

        presentation=new WeaponPresentationSystem(actor.transform,definition);
        presentations.Add(weaponId,presentation);
        return presentation;
    }

    /// <summary>
    /// 解析射击方向
    /// </summary>
    /// <param name="origin">射击起点</param>
    /// <returns>射击方向向量</returns>
    private Vector3 ResolveFireDirection(Vector3 origin)
    {
        // 获取瞄准目标位置
        Vector3 target=actor.runTimeData.aim.TargetPosition;
        Vector3 direction=target-origin;
        // 如果方向有效，返回归一化方向
        if(direction.sqrMagnitude>0.000001f)
            return direction.normalized;

        // 否则使用瞄准核心或角色前方向
        return actor.aimingCore!=null
            ?actor.aimingCore.forward
            :actor.transform.forward;
    }

    /// <summary>
    /// 添加最近事件到环形缓冲区
    /// </summary>
    /// <param name="shotEvent">射击事件数据</param>
    private void AddRecentEvent(in ShotData shotEvent)
    {
        // 如果缓冲区未满，直接添加
        if(recentEventCount<recentEvents.Length)
        {
            int index=(recentEventStart+recentEventCount)%recentEvents.Length;
            recentEvents[index]=shotEvent;
            recentEventCount++;
            return;
        }

        // 如果缓冲区已满，覆盖最早的事件
        recentEvents[recentEventStart]=shotEvent;
        recentEventStart=(recentEventStart+1)%recentEvents.Length;
    }

    /// <summary>
    /// 将射击事件加入展示队列
    /// </summary>
    /// <param name="shotEvent">射击事件数据</param>
    private void QueuePresentationEvent(in ShotData shotEvent)
    {
        // 加入展示队列
        pendingPresentationEvents.Enqueue(shotEvent);
        // 如果是生成事件，也加入动画队列
        if(shotEvent.EventType==ShotEventType.Spawn)
            pendingFireAnimations.Enqueue(shotEvent);
    }
}
