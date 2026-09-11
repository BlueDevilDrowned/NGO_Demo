using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(WorldItemPickup))]
public sealed class SceneItemPlacement : NetworkBehaviour
{
    [SerializeField] private InventoryItemDefinition item;
    [SerializeField] private InventoryItemRegistry registry;

    public InventoryItemDefinition Item => item;
    public InventoryItemRegistry Registry => registry;

    public override void OnNetworkSpawn()
    {
        if (item == null || registry == null) return;

        WorldItemPickup pickup = GetComponent<WorldItemPickup>();
        if (!registry.TryGetId(item, out string id))
        {
            Debug.LogError($"Scene item {item.name} is not registered in {registry.name}.", this);
            return;
        }

        if (IsServer)
            pickup.Initialize(id, registry);
        else
            pickup.BindLocal(item, registry);
    }

    private void OnValidate()
    {
        if (item == null || registry == null) return;
        if (!registry.TryGetId(item, out _))
            Debug.LogWarning($"Scene item {item.name} is not registered in {registry.name}.", this);
    }
}
