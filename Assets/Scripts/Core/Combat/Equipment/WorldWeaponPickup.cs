using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

// 使用多个RequireComponent特性确保游戏对象具有必要的网络和物理组件
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
// 定义一个密封的武器拾取类，继承自NetworkBehaviour
public sealed class WorldWeaponPickup : NetworkBehaviour,IRayInteractable
{
    // 序列化字段，用于在编辑器中设置初始武器ID，并设置最小值为1
    [SerializeField, Min(1)] private int initialWeaponId = 1;
    // 序列化字段，用于存储视觉效果的根变换
    [SerializeField] private Transform visualRoot;
    // 序列化字段，用于存储物理刚体组件
    [SerializeField] private Rigidbody physicsBody;

    public bool CanShow(Actor actor)
    {
        return IsSpawned;
    }

    public void OnLookEnter(Actor actor)
    {
        //先不管
        print("HaveLooked");
    }

    public void OnLookExit(Actor actor)
    {
        
    }

    public bool CanInteract(Actor actor)
    {
       return IsSpawned;
    }

    public void OnInteractSever(Actor actor)
    {
        print("interact");
        actor.weaponEquipment.UnEquipAuthoritative();
        actor.weaponEquipment.EquipInitial(WeaponCatalog.Get((ushort)initialWeaponId));
        DespawnServer(false);
    }

    // 网络变量，用于同步武器ID，只有服务器可以写入，所有人可以读取
    private readonly NetworkVariable<ushort> weaponId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // 武器实例的视觉表示
    private WeaponInstance visualInstance;

    // 属性：获取当前武器ID
    // 如果对象已生成，返回网络变量中的值，否则返回初始值（确保在1到ushort最大值之间）
    public ushort WeaponId => IsSpawned
        ? weaponId.Value
        : (ushort)Mathf.Clamp(initialWeaponId, 1, ushort.MaxValue);

    // 网络生成时的回调函数
    public override void OnNetworkSpawn()
    {
        // 注册武器ID变化事件
        weaponId.OnValueChanged += OnWeaponIdChanged;

        // 如果是服务器且武器ID为0，则设置初始武器ID
        if (IsServer && weaponId.Value == 0)
            weaponId.Value = (ushort)Mathf.Clamp(
                initialWeaponId,
                1,
                ushort.MaxValue);

        // 刷新视觉效果
        RefreshVisual(weaponId.Value);
    }

    // 网络销毁时的回调函数
    public override void OnNetworkDespawn()
    {
        // 取消注册武器ID变化事件
        weaponId.OnValueChanged -= OnWeaponIdChanged;
        // 销毁视觉效果
        DestroyVisual();
    }

    // 设置武器ID的方法
    public void SetWeaponId(ushort value)
    {
        // 参数验证：武器ID不能为0
        if (value == 0)
            throw new System.ArgumentOutOfRangeException(nameof(value));
        // 验证权限：只有已生成的对象的服务器才能修改武器ID
        if (IsSpawned && !IsServer)
            throw new System.InvalidOperationException("Only the server can change a spawned pickup.");

        // 更新初始武器ID
        initialWeaponId = value;
        // 如果对象已生成，同步更新网络变量
        if (IsSpawned)
            weaponId.Value = value;
    }

    // 静态方法：在世界中生成武器拾取物
    public static WorldWeaponPickup Spawn(
        ushort weaponId,
        Vector3 position,
        Quaternion rotation,
        Vector3 linearVelocity)
    {
        // 获取网络管理器并验证是否为服务器
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer)
        {
            Debug.LogError("Only the server can spawn a world weapon pickup.");
            return null;
        }

        // 获取武器定义并验证预制体是否存在
        WeaponSO definition = WeaponCatalog.Get(weaponId);
        if (definition == null || definition.WorldPickupPrefab == null)
        {
            Debug.LogError($"Weapon {weaponId} has no world pickup prefab.");
            return null;
        }

        // 实例化武器拾取物预制体
        WorldWeaponPickup pickup = Instantiate(
            definition.WorldPickupPrefab,
            position,
            rotation);
        // 设置武器ID并生成网络对象
        pickup.SetWeaponId(weaponId);
        pickup.NetworkObject.Spawn();

        // 如果物理刚体存在，设置线性速度
        if (pickup.physicsBody != null)
            pickup.physicsBody.linearVelocity = linearVelocity;

        return pickup;
    }
    public void DespawnServer(bool destroyObject = true)
    {
        if (!IsServer || !IsSpawned)
            return;

        NetworkObject.Despawn(destroyObject);
    }

    // 私有方法：武器ID变化时的回调
    private void OnWeaponIdChanged(ushort previousValue, ushort newValue)
    {
        RefreshVisual(newValue);
    }

    // 私有方法：刷新视觉效果
    private void RefreshVisual(ushort value)
    {
        // 销毁现有视觉效果
        DestroyVisual();
        // 如果武器ID为0或视觉根不存在，则直接返回
        if (value == 0 || visualRoot == null)
            return;

        // 获取武器定义并验证预制体是否存在
        WeaponSO definition = WeaponCatalog.Get(value);
        if (definition == null || definition.Prefab == null)
            return;

        // 实例化新的视觉效果
        visualInstance = Instantiate(definition.Prefab, visualRoot, false);
        visualInstance.Initialize(definition);
    }

    // 私有方法：销毁视觉效果
    private void DestroyVisual()
    {
        // 如果视觉效果不存在，直接返回
        if (visualInstance == null)
            return;

        // 销毁视觉效果游戏对象
        Destroy(visualInstance.gameObject);
        visualInstance = null;
    }

    // 私有方法：编辑器模式下的验证
    private void OnValidate()
    {
        // 确保初始武器ID在有效范围内
        initialWeaponId = Mathf.Clamp(initialWeaponId, 1, ushort.MaxValue);
        // 如果视觉根未设置，则使用当前对象的变换
        if (visualRoot == null)
            visualRoot = transform;
        // 如果物理刚体未设置，则获取组件
        if (physicsBody == null)
            physicsBody = GetComponent<Rigidbody>();
    }



}
