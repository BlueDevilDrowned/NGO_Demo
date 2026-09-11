using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class WorldWeaponPickup : WorldItemPickup
{
    [SerializeField, Min(1)] private int initialWeaponId = 1;
    [SerializeField] private Rigidbody physicsBody;

    private readonly NetworkVariable<ushort> weaponId = new(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public ushort WeaponId => IsSpawned
        ? weaponId.Value
        : (ushort)Mathf.Clamp(initialWeaponId, 1, ushort.MaxValue);

    public override void Initialize(string id, InventoryItemRegistry registry)
    {
        base.Initialize(id, registry);
        if (!registry.TryFindByInfo<WeaponModuleInfo>(Definition.GetInfo<WeaponModuleInfo>()?.Weapon,
                out InventoryItemDefinition item) || item != Definition)
            throw new InvalidOperationException($"Item {id} is not a weapon item.");
    }

    public InventoryItemDefinition WeaponItemDefinition
    {
        get
        {
            WeaponSO weapon=WeaponCatalog.Get(WeaponId);
            InventoryItemRegistry registry=WeaponCatalog.ItemRegistry;
            return weapon!=null&&registry!=null&&
                   registry.TryFindByInfo<WeaponModuleInfo>(weapon,out InventoryItemDefinition item)
                ?item
                :null;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && weaponId.Value == 0)
        {
            weaponId.Value = (ushort)Mathf.Clamp(
                initialWeaponId,
                1,
                ushort.MaxValue);
        }
        // Legacy prefabs keep their weapon ID only to locate their item definition.
        if(Definition==null && WeaponItemDefinition!=null)
        {
            if(IsServer) Initialize(WeaponItemDefinition,WeaponCatalog.ItemRegistry);
            else BindLocal(WeaponItemDefinition,WeaponCatalog.ItemRegistry);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        // Runtime-spawned pickups are destroyed by NGO. Scene objects remain in
        // the scene, so hide them locally on every peer after the despawn message.
        if (NetworkObject.InScenePlaced)
            gameObject.SetActive(false);
    }

    public void SetWeaponId(ushort value)
    {
        if (value == 0)
            throw new System.ArgumentOutOfRangeException(nameof(value));

        if (IsSpawned && !IsServer)
            throw new System.InvalidOperationException(
                "Only the server can change a spawned pickup.");

        initialWeaponId = value;
        if (IsSpawned)
            weaponId.Value = value;
    }

    public static WorldWeaponPickup Spawn(
        ushort weaponId,
        Vector3 position,
        Quaternion rotation,
        Vector3 linearVelocity)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer)
        {
            Debug.LogError("Only the server can spawn a world weapon pickup.");
            return null;
        }

        WeaponSO weapon = WeaponCatalog.Get(weaponId);
        InventoryItemRegistry registry=WeaponCatalog.ItemRegistry;
        if (weapon == null || registry == null ||
            !registry.TryFindByInfo<WeaponModuleInfo>(weapon,out InventoryItemDefinition item))
        {
            Debug.LogError($"Weapon {weaponId} is not mapped to an inventory item.");
            return null;
        }

        DropModuleInfo drop=item.GetInfo<DropModuleInfo>();
        if(drop?.DropPrefab==null)
        {
            Debug.LogError($"Item {item.name} has no drop prefab.");
            return null;
        }

        GameObject dropObject = Instantiate(
            drop.DropPrefab,
            position,
            rotation);
        WorldWeaponPickup pickup=dropObject.GetComponent<WorldWeaponPickup>();
        if(pickup==null)
        {
            Destroy(dropObject);
            Debug.LogError($"Drop prefab for {item.name} has no WorldWeaponPickup component.");
            return null;
        }
        pickup.SetWeaponId(weaponId);
        pickup.Initialize(registry.GetId(item), registry);
        pickup.NetworkObject.Spawn();

        // NetworkRigidbody may finalize its authority/kinematic state during
        // NetworkObject.Spawn. Apply the launch velocity only afterwards.
        if (pickup.physicsBody != null)
        {
            pickup.physicsBody.isKinematic=false;
            pickup.physicsBody.linearVelocity=linearVelocity;
        }

        return pickup;
    }

    public void DespawnServer()
    {
        if (!IsServer || !IsSpawned)
            return;

        NetworkObject.Despawn(!NetworkObject.InScenePlaced);
    }

    private void OnValidate()
    {
        initialWeaponId = Mathf.Clamp(initialWeaponId, 1, ushort.MaxValue);

        if (physicsBody == null)
            physicsBody = GetComponent<Rigidbody>();
    }
}
