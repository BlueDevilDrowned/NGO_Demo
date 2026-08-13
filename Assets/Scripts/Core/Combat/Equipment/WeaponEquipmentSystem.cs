using System;
using UnityEngine;

/// <summary>
/// 封装武器装备系统的类，实现IDisposable接口以支持资源释放
/// </summary>
public sealed class WeaponEquipmentSystem : IDisposable
{
    // 武器骨骼控制器，用于控制武器的挂载和绑定
    private readonly WeaponRigController rigController;
    // 用于存储武器定义的字典，以武器ID作为键

    /// <summary>
    /// 当前装备的武器实例
    /// </summary>
    public WeaponInstance CurrentWeapon{get;private set;}
    /// <summary>
    /// 获取当前武器的定义数据
    /// </summary>
    public WeaponSO CurrentDefinition=>CurrentWeapon?.Definition;
    /// <summary>
    /// 获取当前武器的ID
    /// </summary>
    public ushort CurrentWeaponId=>CurrentDefinition!=null
        ?CurrentDefinition.Id
        :(ushort)0;
    /// <summary>
    /// 获取当前武器的枪口位置
    /// </summary>
    public Transform Muzzle=>CurrentWeapon?.Muzzle;

    /// <summary>
    /// 武器变更事件，当武器装备或卸载时触发
    /// </summary>
    public event Action<WeaponInstance> WeaponChanged;
    private Actor actor;
    /// <summary>
    /// 构造函数，初始化武器装备系统
    /// </summary>
    /// <param name="rigController">武器骨骼控制器</param>
    /// <param name="definitions">武器定义集合</param>

    public WeaponEquipmentSystem(Actor actor)
    {
        this.actor=actor;
        this.rigController=actor.weaponRigController??
            throw new ArgumentNullException(nameof(rigController));
    }

    /// <summary>
    /// 装载初始武器
    /// </summary>
    /// <param name="definition">武器定义</param>
    /// <returns>是否成功装载</returns>
    internal bool EquipInitial(WeaponSO definition)
    {
        if(!ValidateDefinition(definition))return false;

        // 检查是否已存在挂载的武器实例
        WeaponInstance mounted=rigController.WeaponMount!=null
            ?rigController.WeaponMount.GetComponentInChildren<WeaponInstance>(true)
            :null;
        if(mounted==null)
            return EquipLocal(definition);

        // 初始化并绑定已存在的武器实例
        mounted.Initialize(definition);
        if(!mounted.IsValid()||!rigController.Bind(mounted))return false;

        CurrentWeapon=mounted;
        WeaponChanged?.Invoke(CurrentWeapon);
        return true;
    }

    /// <summary>
    /// 权限端装备武器
    /// </summary>
    /// <param name="definition">武器定义</param>
    /// <returns>是否成功装备</returns>
    internal bool EquipAuthoritative(WeaponSO definition)
    {
        return EquipLocal(definition);
    }

    /// <summary>
    /// 应用权限端武器变更
    /// </summary>
    /// <param name="weaponId">武器ID</param>
    /// <returns>是否成功应用</returns>
    internal bool ApplyAuthoritativeWeapon(ushort weaponId)
    {
        if(weaponId==CurrentWeaponId)return true;
        if(weaponId==0)
        {
            UnEquipLocal();
            return true;
        }

        WeaponSO definition=WeaponCatalog.Get(weaponId);
        if(definition==null)return false;

        return EquipLocal(definition);
    }

    /// <summary>
    /// 权限端卸载武器
    /// </summary>
    internal void UnEquipAuthoritative()
    {
        UnEquipLocal();
    }

    private bool EquipLocal(WeaponSO definition)
    {
        if(!ValidateDefinition(definition))return false;
        if(CurrentDefinition==definition&&CurrentWeapon!=null)return true;

        UnEquipLocal();
        WeaponInstance instance=UnityEngine.Object.Instantiate(
            definition.Prefab,
            rigController.WeaponMount,
            false);
        instance.Initialize(definition);
        if(!instance.IsValid()||!rigController.Bind(instance))
        {
            UnityEngine.Object.Destroy(instance.gameObject);
            return false;
        }

        CurrentWeapon=instance;
        WeaponChanged?.Invoke(CurrentWeapon);
        return true;
    }

    private void UnEquipLocal()
    {
        if(CurrentWeapon==null)return;

        WeaponInstance oldWeapon=CurrentWeapon;
        CurrentWeapon=null;
        rigController.Unbind();
        UnityEngine.Object.Destroy(oldWeapon.gameObject);
        WeaponChanged?.Invoke(null);

        WorldWeaponPickup.Spawn(oldWeapon.Definition.Id,oldWeapon.RightHandGrip.position,oldWeapon.RightHandGrip.rotation,Vector3.zero);
    }

    public void Dispose()
    {
        UnEquipLocal();
        WeaponChanged=null;
    }

    private static bool ValidateDefinition(WeaponSO definition)
    {
        if(definition==null||definition.Id==0||definition.Prefab==null)
        {
            Debug.LogError("Weapon definition requires a non-zero ID and a prefab.");
            return false;
        }

        return true;
    }
}
