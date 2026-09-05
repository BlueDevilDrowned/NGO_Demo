using System;
using Animancer;
using UnityEngine;
/// <summary>
/// 武器系统类，实现了IProjectileEventSink接口，用于处理武器射击相关逻辑
/// </summary>
public sealed class WeaponSystem : IActorSystem,IProjectileEventSink
{
    private const int FireAnimationLayer=1;
    // 持有武器系统的角色引用
    public Actor actor;
    private readonly WeaponEquipmentSystem equipment;
    private readonly ProjectileSystem projectiles;
    private readonly WeaponReplication replication;
    private readonly WeaponPresentationSystem presentation;

    // 下次射击的游戏刻度
    private uint nextFireTick;
    // 事件序列号
    private uint eventSequence;
    // 最后应用的事件序列号
    private uint lastAppliedEventSequence;
    private bool isDisposed;

    // 最后一次射击数据（只读）
    public ShotData LastShot{get;private set;}

    /// <summary>
    /// 武器系统构造函数
    /// </summary>
    /// <param name="actor">持有该武器系统的角色</param>
    public WeaponSystem(Actor actor,WeaponEquipmentSystem equipment)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        this.equipment=equipment??
            throw new ArgumentNullException(nameof(equipment));
        projectiles=new(actor,this);
        replication=new(actor);
        if(actor.IsClient)
            presentation=new(actor);
        this.equipment.WeaponChanged+=OnWeaponChanged;
        actor.RegisterSystem(this);
    }

    /// <summary>
    /// 尝试射击武器，计算射击间隔
    /// </summary>
    /// <returns>是否成功射击</returns>
    public bool TryFire()
    {
    ///前置
        if(isDisposed)return false;
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
    ///正式开始射击逻辑
        // 获取当前服务器刻度
        uint currentServerTick=actor.serverTick;
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
            ShotTick=currentServerTick,
            FireIntervalTicks=fireIntervalTicks,
            WeaponId=definition.Id,
            Damage=definition.Damage,
            Speed=definition.TracerSpeed,
            Gravity=definition.ProjectileGravity,
            Range=definition.Range,
            HitMask=definition.HitMask,
            Origin=origin,
            Direction=direction,
        };
        // 生成投射物
        uint projectileId=projectiles.Spawn(in spawnData);
        if(projectileId==0)return false;

        // 更新下次射击刻度
        nextFireTick=currentServerTick+fireIntervalTicks;
        return true;
    }

    /// <summary>
    /// 更新子弹
    /// </summary>
    /// <param name="currentServerTick"></param>
    public void ServerTick(uint currentServerTick)
    {
        if(isDisposed||!actor.IsServer)return;

        projectiles.ServerTick(currentServerTick,TickTime.deltaTime);
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
        replication.EnqueueAuthoritativeEvent(in authoritativeEvent);
    }

    /// <summary>
    /// 应用权威射击事件，将权威快照加入到展示队列中
    /// </summary>
    /// <param name="authoritativeEvent">权威射击事件数据</param>
    private void ApplyAuthoritativeShot(in ShotData authoritativeEvent)
    {
        // 检查事件序列是否有效
        if(authoritativeEvent.Sequence==0||
           authoritativeEvent.Sequence<=lastAppliedEventSequence)return;

        // 更新最后应用的事件序列
        lastAppliedEventSequence=authoritativeEvent.Sequence;
        LastShot=authoritativeEvent;
        ApplyPresentation(in authoritativeEvent);
    }

    /// <summary>
    /// 消费服务器事件并更新射击表现。
    /// </summary>
    public void PresentationUpdate()
    {
        if(isDisposed)return;
        //从服务器传出的快照中去除，加入展示队列中
        while(replication.TryConsumeEvent(out ShotData authoritativeEvent))
            ApplyAuthoritativeShot(in authoritativeEvent);

    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        equipment.WeaponChanged-=OnWeaponChanged;
        replication.Dispose();
        projectiles.Clear();
        presentation?.Dispose();
        actor.firstPersonAnimationFacade?.ClearOnEndCallBack(
            FireAnimationLayer);
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

        float ticksPerShot=(float)TickTime.TickRate/definition.FireRate;//射击间隔夺少需要tick
        return (uint)Mathf.Max(1,Mathf.CeilToInt(ticksPerShot));
    }

    private void OnWeaponChanged(WeaponInstance _)
    {
        nextFireTick=0;
        actor.firstPersonAnimationFacade?.ClearOnEndCallBack(
            FireAnimationLayer);
        actor.firstPersonAnimationFacade?.StopLayer(
            FireAnimationLayer);
        actor.firstPersonAnimationFacade?.SetLayerWeight(
            FireAnimationLayer,
            0f);
        if(actor.IsClient&&equipment?.CurrentDefinition!=null)
            presentation?.Prepare(equipment.CurrentDefinition.Id);
    }

    private void PlayFirstPersonFireAnimation(in ShotData shot)
    {
        if(!actor.IsOwner||actor.firstPersonAnimationFacade==null)return;

        // Authoritative events can arrive after a local weapon switch. Never
        // interpret an old weapon event with the newly equipped weapon config.
        if(equipment?.CurrentWeaponId!=shot.WeaponId||
           !WeaponCatalog.TryGet(shot.WeaponId,out WeaponSO definition))
            return;

        WeaponAnimationSO animationConfig=definition.animationConfig;
        FirstPersonWeaponCombatAnimations animations=
            animationConfig?.FirstPerson?.Combat;
        if(animations==null)return;

        bool aiming=actor.aimSystem?.IsAiming==true;
        TransitionAsset transition=aiming
            ?animations.AimAttack??animations.Attack
            :animations.Attack;
        if(transition==null)return;

        float intervalSeconds=shot.FireIntervalTicks/
                              (float)TickTime.TickRate;
        ITransition animancerTransition=transition;
        float animationLength=animancerTransition.MaximumLength;
        float animationSpeed=intervalSeconds>Mathf.Epsilon&&
                             animationLength>Mathf.Epsilon&&
                             !float.IsInfinity(animationLength)&&
                             !float.IsNaN(animationLength)
            ?animationLength/intervalSeconds
            :1f;

        AnimPlayOptions options=AnimPlayOptions.Default;
        options.Layer=FireAnimationLayer;
        options.FadeDuration=0f;
        options.NormalizedTime=0f;
        options.Speed=animationSpeed;
        actor.firstPersonAnimationFacade.PlayTransition(transition,options);
        actor.firstPersonAnimationFacade.SetLayerWeight(
            FireAnimationLayer,
            1f,
            0.1f);
        actor.firstPersonAnimationFacade.SetOnEndCallback(
            HandleFirstPersonFireAnimationEnd,
            FireAnimationLayer);
    }

    private void HandleFirstPersonFireAnimationEnd()
    {
        actor.firstPersonAnimationFacade?.SetLayerWeight(
            FireAnimationLayer,
            0f,
            0.1f);
    }

    /// <summary>
    /// 解析射击方向,通过target计算
    /// </summary>
    /// <param name="origin">射击起点</param>
    /// <returns>射击方向向量</returns>
    private Vector3 ResolveFireDirection(Vector3 origin)
    {
        Vector3 target=actor.simulation.aimData.TargetPosition;
        Vector3 direction=target-origin;
        if(direction.sqrMagnitude>0.000001f)
            return direction.normalized;

        return actor.transform.forward;
    }

    private void ApplyOwnerCameraRecoil(in ShotData shot)
    {
        if(!actor.IsOwner||actor.cameraSystem==null||
           !WeaponCatalog.TryGet(shot.WeaponId,out WeaponSO definition))
            return;

        float speed=Mathf.Max(0f,definition.FireCameraRecoilSpeed);
        if(speed<=Mathf.Epsilon)return;

        float azimuth=UnityEngine.Random.value*Mathf.PI*2f;
        CameraRecoilRequest request=new(
            "WeaponFire",
            Mathf.Cos(azimuth)*speed,
            Mathf.Sin(azimuth)*speed);
        actor.cameraSystem.SubmitRecoil(in request);
    }

    private void ApplyPresentation(in ShotData shotEvent)
    {
        presentation?.Apply(in shotEvent);
        if(shotEvent.EventType!=ShotEventType.Spawn)return;

        PlayFirstPersonFireAnimation(in shotEvent);
        ApplyOwnerCameraRecoil(in shotEvent);
        if(WeaponCatalog.TryGet(shotEvent.WeaponId,out WeaponSO definition))
            actor.audioSystem.PlayOneShot(definition.FireAudio);
    }
}
