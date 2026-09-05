using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NetworkRigidbody))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class WorldWeaponPickup : NetworkBehaviour, IRayInteractable
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

    public bool CanShow(Actor actor)
    {
        return IsSpawned;
    }

    public void OnLookEnter(Actor actor)
    {
        Debug.Log("HaveLooked");
    }

    public void OnLookExit(Actor actor)
    {
    }

    public bool CanInteract(Actor actor)
    {
        return IsSpawned;
    }

    public void OnInteractServer(Actor actor)
    {
        Debug.Log("TryInteract");
        if (!IsServer || actor == null)
            return;

        if(actor.weaponInventory==null||
           !actor.weaponInventory.TryPickupWeapon(
               WeaponId,
               out _,
               out ushort replacedWeaponId))
        {
            return;
        }
        Debug.Log("InteractSuccessfull");
        if(replacedWeaponId>0)
        {
            Vector3 inheritedVelocity=actor.movement?.Velocity??Vector3.zero;
            float throwSpeed=actor.actorSO?.controllerSO?.WeaponDropThrowSpeed??0f;
            Vector3 dropVelocity=inheritedVelocity+
                actor.transform.forward*throwSpeed;
            ControllerSO controller=actor.actorSO?.controllerSO;
            Vector3 dropPosition=controller!=null
                ?controller.GetWeaponDropPosition(actor.transform)
                :actor.transform.position;

            WorldWeaponPickup.Spawn(
                replacedWeaponId,
                dropPosition,
                actor.transform.rotation,
                dropVelocity);
        }

        DespawnServer();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && weaponId.Value == 0)
        {
            weaponId.Value = (ushort)Mathf.Clamp(
                initialWeaponId,
                1,
                ushort.MaxValue);
        }
    }

    public override void OnNetworkDespawn()
    {
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

        WeaponSO definition = WeaponCatalog.Get(weaponId);
        if (definition == null || definition.WorldPickupPrefab == null)
        {
            Debug.LogError($"Weapon {weaponId} has no world pickup prefab.");
            return null;
        }

        WorldWeaponPickup pickup = Instantiate(
            definition.WorldPickupPrefab,
            position,
            rotation);

        pickup.SetWeaponId(weaponId);
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
