using System;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

/// <summary>通用物品掉落实体的世界表现基类。网络实体由具体拾取组件负责。</summary>
[RequireComponent(typeof(NetworkObject))]
public class WorldItemPickup : NetworkBehaviour, IRayInteractable, IInteractionOptionProvider
{
    private string itemId;
    private InventoryItemDefinition definition;
    protected readonly NetworkVariable<FixedString64Bytes> networkItemId = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public string ItemId => IsSpawned ? networkItemId.Value.ToString() : itemId;
    public InventoryItemDefinition Definition => definition;
    public override void OnNetworkSpawn()
    {
        networkItemId.OnValueChanged+=OnItemChanged;
        if(IsServer && !string.IsNullOrEmpty(itemId)) networkItemId.Value=itemId;
        ResolveDefinition();
    }
    public override void OnNetworkDespawn()
    {
        networkItemId.OnValueChanged-=OnItemChanged;
        if(NetworkObject.InScenePlaced) gameObject.SetActive(false);
    }
    private void OnItemChanged(FixedString64Bytes previous, FixedString64Bytes current) => ResolveDefinition();
    private void ResolveDefinition()
    {
        if(!string.IsNullOrEmpty(ItemId)) definition=WeaponCatalog.ItemRegistry?.Find(ItemId) ?? definition;
    }
    public void ConsumeServer()
    {
        if(!IsServer || !IsSpawned) return;
        bool scene=NetworkObject.InScenePlaced;
        NetworkObject.Despawn(!scene);
        if(scene) gameObject.SetActive(false);
    }

    public virtual void Initialize(string id, InventoryItemRegistry registry)
    {
        if (registry == null) throw new ArgumentNullException(nameof(registry));
        InventoryItemDefinition item = registry.Find(id);
        if (item == null) throw new ArgumentException($"Unknown item ID: {id}", nameof(id));
        itemId = id;
        definition = item;
        if (IsSpawned)
            networkItemId.Value = id;
    }

    public virtual void Initialize(InventoryItemDefinition item, InventoryItemRegistry registry)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (registry == null) throw new ArgumentNullException(nameof(registry));
        if (!registry.TryGetId(item, out string id))
            throw new ArgumentException($"Item is not registered: {item.name}", nameof(item));
        Initialize(id, registry);
    }

    public void BindLocal(InventoryItemDefinition item, InventoryItemRegistry registry)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));
        if (registry == null) throw new ArgumentNullException(nameof(registry));
        if (!registry.TryGetId(item, out string id))
            throw new ArgumentException($"Item is not registered: {item.name}", nameof(item));
        itemId = id;
        definition = item;
    }

    public virtual bool CanShow(Actor actor) => IsSpawned;
    public virtual void OnLookEnter(Actor actor)
    {
        if(definition?.Modules==null) return;
        foreach(var module in definition.Modules)
            if(module!=null && module.CanShow(definition,actor)) module.OnShow(definition,actor);
    }
    public virtual void OnLookExit(Actor actor) { }
    public virtual bool CanInteract(Actor actor) => IsSpawned;
    public virtual void OnInteractServer(Actor actor)
    {
        if(actor==null || !actor.IsServer) return;
        var input=actor.simulation.inputData;
        if(input.InteractionTarget==NetworkObjectId && !input.InteractionOption.IsEmpty)
            ExecuteInteraction(input.InteractionOption.ToString(),actor);
    }

    public virtual IReadOnlyList<ItemInteractionOption> GetInteractionOptions(Actor actor)
    {
        var result = new List<ItemInteractionOption>();
        if (definition != null)
            new ItemInstance(ItemId, definition).CollectInteractionOptions(actor, result);
        return result;
    }

    public virtual bool ExecuteInteraction(string optionId, Actor actor)
    {
        if(!IsServer || !IsSpawned || actor==null || definition==null) return false;
        if(!new ItemInstance(ItemId,definition).ExecuteInteraction(optionId,actor)) return false;
        ConsumeServer();
        return true;
    }
}


