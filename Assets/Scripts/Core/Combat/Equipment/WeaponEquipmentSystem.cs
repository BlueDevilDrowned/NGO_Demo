using System;
using UnityEngine;
/// <summary>
/// 装备系统，目前只限于枪，客户端预测，服务器则同步权威操作，客户端根据信息，决定是否矫正
/// </summary>
public sealed class WeaponEquipmentSystem : IActorSystem
{
    public Actor actor;
    public WeaponEquipmentData data;
    public WeaponEquipmentReplication replication;

    public WeaponInstance CurrentWeapon=>actor.weaponRig.CurrentWeapon;
    public ushort CurrentWeaponId=>data.id>0?(ushort)data.id:(ushort)0;
    public WeaponSO CurrentDefinition=>CurrentWeaponId>0?WeaponCatalog.Get(CurrentWeaponId):null;
    public Transform Muzzle=>CurrentWeapon?.Muzzle;

    public event Action<WeaponInstance> WeaponChanged;

    private bool isDisposed;
    private bool hasPendingPrediction;
    private uint pendingPredictionTick;
    private int predictedWeaponId=-1;

    public WeaponEquipmentSystem(Actor actor,int initialWeaponId=-1)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        if(actor.weaponRig==null)
            throw new ArgumentNullException(nameof(actor.weaponRig));

        data=WeaponEquipmentData.NoWeapon();
        if(actor.IsServer)
            actor.simulation.weaponEquipmentData=data;

        replication=new(actor);
        actor.RegisterSystem(this);
        Initialize(initialWeaponId);
    }

    private bool isInitialized;
    /// <summary>
    /// 初始化时可以传入id。如果传入自带武器
    /// </summary>
    /// <param name="weaponId"></param>
    /// <returns></returns>
    public bool Initialize(int weaponId=-1)
    {
        if(isInitialized)return false;

        isInitialized=true;
        if(weaponId>=0)
        {
            bool equipped=ApplyEquip(weaponId);
            if(equipped&&actor.IsServer)
                ConfirmAuthoritativeResult(0);
            return equipped;
        }

        if(actor.IsServer)
            ConfirmAuthoritativeResult(0);
        return true;
    }
    /// <summary>
    /// 装，服务器权威与客户端预测走了同一个通道，但是服务器会同步操作数据，客户端则是预测，之后会根据服务器数据矫正
    /// </summary>
    /// <param name="weaponId"></param>
    /// <returns></returns>
    public bool Equip(int weaponId)
    {
        bool equipped=ApplyEquip(weaponId);
        if(equipped&&actor.IsServer)
            ConfirmAuthoritativeResult(
                actor.inputSystem.replication.LastReceivedInputTick);
        else if(equipped&&actor.IsOwner)
            BeginPrediction(weaponId);
        return equipped;
    }

    private bool ApplyEquip(int weaponId)
    {
        if(isDisposed||weaponId<=0||weaponId>ushort.MaxValue)return false;
        if(data.id==weaponId&&CurrentWeapon!=null)return true;
        //通过id得到数据
        if(!WeaponCatalog.TryGet((ushort)weaponId,out WeaponSO definition)||
           definition.Prefab==null||actor.weaponRig.WeaponMount==null)
            return false;

        WeaponInstance oldWeapon=CurrentWeapon;
        //表现层直接创建武器实例
        WeaponInstance newWeapon=UnityEngine.Object.Instantiate(
            definition.Prefab,
            actor.weaponRig.WeaponMount,
            false);

        //数据如果合法，则执行绑定；如果不合法，或者绑定失败，则取消
        if(!newWeapon.IsValid()||!actor.weaponRig.Bind(newWeapon))
        {
            UnityEngine.Object.Destroy(newWeapon.gameObject);
            return false;
        }
        //摧毁旧实例
        if(oldWeapon!=null&&oldWeapon!=newWeapon)
            UnityEngine.Object.Destroy(oldWeapon.gameObject);

        data.id=weaponId;
        WeaponChanged?.Invoke(newWeapon);
        return true;
    }
    /// <summary>
    /// 卸载，服务器权威与客户端预测走了同一个通道，但是服务器会同步操作数据，客户端则是预测，之后会根据服务器数据矫正
    /// </summary>
    public void Unequip()
    {
        ApplyUnequip();
        if(actor.IsServer&&!isDisposed)
            ConfirmAuthoritativeResult(
                actor.inputSystem.replication.LastReceivedInputTick);
        else if(actor.IsOwner&&!isDisposed)
            BeginPrediction(-1);
    }

    private void ApplyUnequip()
    {
        WeaponInstance oldWeapon=CurrentWeapon;
        bool hadWeapon=data.id>0||oldWeapon!=null;

        actor.weaponRig.Unbind();
        data=WeaponEquipmentData.NoWeapon();

        if(oldWeapon!=null)
            UnityEngine.Object.Destroy(oldWeapon.gameObject);
        if(hadWeapon)
            WeaponChanged?.Invoke(null);
    }
    
    /// <summary>
    /// 记录预测tick（实际只保留最后一次操作的tick，理论上不停止操作，服务器会一直追但是追不上），之后收到权威数据，会进行矫正
    /// </summary>
    /// <param name="weaponId"></param>
    private void BeginPrediction(int weaponId)
    {
        uint inputTick=actor.localTick;
        if(!hasPendingPrediction||inputTick>=pendingPredictionTick)
        {
            hasPendingPrediction=true;
            pendingPredictionTick=inputTick;
            predictedWeaponId=weaponId;
        }
    }
    /// <summary>
    /// 同步服务器的操作，客户端矫正自己的预测
    /// </summary>
    /// <param name="processedInputTick"></param>
    public void ConfirmAuthoritativeResult(uint processedInputTick)
    {
        if(!actor.IsServer)return;

        replication.MarkAuthoritativeState(data,processedInputTick);
    }
    /// <summary>
    /// 消费已经接受的数据，tick>=上次预测的tick则尝试纠正表现，
    /// </summary>
    public void PresentationUpdate()
    {
        if(!replication.TryConsumeState(out WeaponEquipmentSnapshot snapshot))
            return;

        if(actor.IsServer)return;

        int authoritativeId=snapshot.data.id;
        if(actor.IsOwner&&hasPendingPrediction)
        {
            if(snapshot.ProcessedInputTick<pendingPredictionTick)return;

            hasPendingPrediction=false;
            if(authoritativeId==predictedWeaponId)return;
        }

        if(authoritativeId>0)
            ApplyEquip(authoritativeId);
        else
            ApplyUnequip();
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        replication.Dispose();
        ApplyUnequip();
        WeaponChanged=null;
    }
}
