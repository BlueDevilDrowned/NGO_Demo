using System;
using InventorySolver;
using UnityEngine;

public sealed class InventorySystem : IActorSystem
{
    private readonly Actor actor;
    private readonly InventoryReplication replication;
    private readonly InventoryPlacementRequestChannel placementRequests;
    private readonly BackpackInteractionRequestChannel interactionRequests;
    private InventoryRuntime runtime;
    public BackpackModule ActiveBackpack { get; private set; }
    public string ActiveBackpackItemId { get; private set; }
    private bool isDisposed;

    public InventoryData Data => actor.simulation.inventoryData;
    public InventoryRuntime Runtime => runtime;
    public Actor Owner => actor;

    public event Action InventoryChanged;

    public InventorySystem(Actor actor, InventoryLayout layout = null)
    {
        this.actor = actor ?? throw new ArgumentNullException(nameof(actor));
        if (actor.simulation.inventoryData == null)
            actor.simulation.inventoryData = new InventoryData();

        if (layout != null)
        {
            runtime = new InventoryRuntime(layout);
            runtime.Changed += OnRuntimeChanged;
        }
        replication = new InventoryReplication(actor);
        placementRequests = new InventoryPlacementRequestChannel(actor);
        interactionRequests = new BackpackInteractionRequestChannel(actor);
        actor.RegisterSystem(this);
    }

    public InventoryRuntime.PlacementPreviewResult PreviewPlacement(
        int instanceId,
        Placement placement)
    {
        if (runtime == null)
            return new InventoryRuntime.PlacementPreviewResult(false, placement, Array.Empty<Cell>());
        return runtime.PreviewPlacement(instanceId, placement);
    }

    public bool TryCommitPlacement(int instanceId, Placement placement)
    {
        if (isDisposed || !actor.IsServer || runtime == null) return false;
        return runtime.TryCommitPlacement(instanceId, placement);
    }

    public bool TryAutoPlace(ItemInstance item, out InventoryRuntime.Entry entry)
    {
        entry = null;
        if (isDisposed || !actor.IsServer || runtime == null) return false;
        return runtime.TryAdd(item, out entry);
    }

    public bool TryEquipBackpack(InventoryLayout layout, BackpackModule module = null, string itemId = null)
    {
        if(isDisposed || !actor.IsServer || layout==null) return false;
        // Do not silently discard a previously equipped backpack or its contents.
        if(runtime!=null) return false;
        runtime=new InventoryRuntime(layout);
        ActiveBackpack = module;
        ActiveBackpackItemId = itemId ?? string.Empty;
        runtime.Changed+=OnRuntimeChanged;
        OnRuntimeChanged();
        return true;
    }
    public void PresentationUpdate()
    {
        if (isDisposed || actor.IsServer) return;
        if (ActiveBackpack == null)
        {
            ResolveBackpackFromData();
        }
        if (replication.TryConsumeState(out _))
        {
            ResolveBackpackFromData();
            InventoryChanged?.Invoke();
        }
    }

    public bool TryDropItem(int instanceId)
    {
        InventoryRuntime.Entry entry = FindEntry(instanceId);
        if (isDisposed || !actor.IsServer || entry == null)
        {
            return false;
        }

        Vector3 velocity = actor.movement?.Velocity ?? Vector3.zero;
        Vector3 position = actor.transform.position + actor.transform.forward;
        WorldItemPickup pickup = WorldItemPickup.SpawnItem(
            entry.Item.Definition,
            position,
            actor.transform.rotation,
            velocity);
        if (pickup == null)
        {
            return false;
        }

        if (runtime.Remove(instanceId))
        {
            return true;
        }

        pickup.ConsumeServer();
        return false;
    }

    public bool TryEquipWeapon(int instanceId, ushort weaponId)
    {
        InventoryRuntime.Entry entry = FindEntry(instanceId);
        if (isDisposed ||
            !actor.IsServer ||
            entry == null ||
            actor.weaponInventory == null ||
            !actor.weaponInventory.TryPickupFromItem(weaponId))
        {
            return false;
        }

        return runtime.Remove(instanceId);
    }

    public bool TryEquipBackpackFromInventory(
        int instanceId,
        BackpackModule backpack)
    {
        InventoryRuntime.Entry equippedEntry = FindEntry(instanceId);
        if (isDisposed ||
            !actor.IsServer ||
            equippedEntry == null ||
            backpack == null)
        {
            return false;
        }

        var replacement = new InventoryRuntime(backpack.CreateLayout());
        foreach (InventoryRuntime.Entry entry in runtime.Entries)
        {
            if (entry.InstanceId == instanceId)
            {
                continue;
            }

            if (!replacement.TryAdd(entry.Item, out _))
            {
                return false;
            }
        }

        if (!string.IsNullOrEmpty(ActiveBackpackItemId))
        {
            ItemInstance previousBackpack =
                WeaponCatalog.ItemRegistry?.CreateInstance(ActiveBackpackItemId);
            if (previousBackpack == null ||
                !replacement.TryAdd(previousBackpack, out _))
            {
                return false;
            }
        }

        runtime.Changed -= OnRuntimeChanged;
        runtime = replacement;
        runtime.Changed += OnRuntimeChanged;
        ActiveBackpack = backpack;
        ActiveBackpackItemId = equippedEntry.Item.ItemId;
        OnRuntimeChanged();
        return true;
    }

    public void RequestBackpackInteraction(int instanceId, string optionId)
    {
        if (isDisposed || !actor.IsOwner || string.IsNullOrEmpty(optionId))
        {
            return;
        }

        interactionRequests.RequestInteraction(instanceId, optionId);
    }

    public bool ExecuteBackpackInteraction(int instanceId, string optionId)
    {
        InventoryRuntime.Entry entry = FindEntry(instanceId);
        if (isDisposed || !actor.IsServer || entry == null)
        {
            return false;
        }

        return entry.Item.ExecuteBackpackInteraction(entry, optionId, actor);
    }

    private InventoryRuntime.Entry FindEntry(int instanceId)
    {
        if (runtime == null)
        {
            return null;
        }

        foreach (InventoryRuntime.Entry entry in runtime.Entries)
        {
            if (entry.InstanceId == instanceId)
            {
                return entry;
            }
        }

        return null;
    }
    public void RequestPlacement(
        int instanceId,
        int regionIndex,
        int x,
        int y,
        InventorySolver.ERotation rotation)
    {
        if (isDisposed || !actor.IsOwner || runtime == null)
        {
            return;
        }

        placementRequests.RequestPlacement(
            instanceId,
            regionIndex,
            x,
            y,
            rotation);
    }

    private void ResolveBackpackFromData()
    {
        if (Data?.entries == null) return;
        if (ActiveBackpack != null)
        {
            runtime?.Restore(Data.entries, WeaponCatalog.ItemRegistry);
            return;
        }
        var registry = WeaponCatalog.ItemRegistry;
        if (registry == null) return;
        if (!string.IsNullOrEmpty(Data.backpackItemId))
        {
            InventoryItemDefinition configured = registry.Find(Data.backpackItemId);
            if (configured != null && configured.Modules != null)
            {
                foreach (ItemModule module in configured.Modules)
                {
                    if (module is BackpackModule backpack)
                    {
                        ActiveBackpackItemId = Data.backpackItemId;
                        ActiveBackpack = backpack;
                        runtime ??= new InventoryRuntime(backpack.CreateLayout());
                        runtime.Restore(Data.entries, registry);
                        return;
                    }
                }
            }
        }
        foreach (var entry in Data.entries)
        {
            var definition = registry.Find(entry.itemId);
            if (definition == null) continue;
            foreach (var module in definition.Modules)
            {
                if (module is BackpackModule backpack)
                {
                    ActiveBackpack = backpack;
                    ActiveBackpackItemId = entry.itemId;
                    runtime ??= new InventoryRuntime(backpack.CreateLayout());
                    runtime.Restore(Data.entries, registry);
                    return;
                }
            }
        }
    }

    public void MarkAuthoritativeState()
    {
        if (isDisposed || !actor.IsServer) return;
        InventoryData data = Data;
        replication.MarkAuthoritativeState(
            in data,
            actor.inputSystem.replication.LastReceivedInputTick);
    }

    public void Dispose()
    {
        if (isDisposed) return;
        isDisposed = true;
        if (runtime != null)
            runtime.Changed -= OnRuntimeChanged;
        replication.Dispose();
        placementRequests.Unregister();
        interactionRequests.Unregister();
        InventoryChanged = null;
    }

    private void OnRuntimeChanged()
    {
        if (actor.IsServer)
            SyncRuntimeToData();
        InventoryChanged?.Invoke();
    }

    private void SyncRuntimeToData()
    {
        InventoryData data = new InventoryData();
        data.backpackItemId = ActiveBackpackItemId ?? string.Empty;
        foreach (InventoryRuntime.Entry entry in runtime.Entries)
        {
            data.entries.Add(new InventoryData.Entry
            {
                instanceId = entry.InstanceId,
                itemId = entry.Item.ItemId,
                placement = entry.Placement
            });
        }

        actor.simulation.inventoryData = data;
        MarkAuthoritativeState();
    }
}
