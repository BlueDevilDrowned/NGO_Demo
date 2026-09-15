using System;
using System.Collections.Generic;

public interface IInteractionOptionProvider
{
    IReadOnlyList<ItemInteractionOption> GetInteractionOptions(Actor actor);
    bool ExecuteInteraction(string optionId, Actor actor);
}
public sealed class ItemInstance
{
    public string ItemId { get; }
    public InventoryItemDefinition Definition { get; }
    public IReadOnlyList<ItemModule> Modules => Definition.Modules;
    public ItemInstance(InventoryItemDefinition definition)
        : this(string.Empty, definition)
    {
    }

    public ItemInstance(string itemId, InventoryItemDefinition definition)
    {
        ItemId = itemId ?? string.Empty;
        Definition = definition ??
            throw new ArgumentNullException(nameof(definition));
    }

    public void CollectInteractionOptions(Actor actor, List<ItemInteractionOption> options)
    {
        if (Modules == null)
        {
            return;
        }

        var local = new List<ItemInteractionOption>();
        for (int i = 0; i < Modules.Count; i++)
        {
            ItemModule module = Modules[i];
            if (module == null || !module.CanShow(Definition, actor))
            {
                continue;
            }

            local.Clear();
            module.CollectInteractionOptions(Definition, local);
            foreach (ItemInteractionOption option in local)
            {
                options.Add(new ItemInteractionOption(
                    i + ":" + option.Id,
                    option.DisplayName,
                    module.CanInteract(Definition, actor),
                    module,
                    option.Icon));
            }
        }
    }

    public bool ExecuteInteraction(string optionId, Actor actor)
    {
        if (actor == null || !actor.IsServer)
        {
            return false;
        }

        var options = new List<ItemInteractionOption>();
        CollectInteractionOptions(actor, options);
        foreach (ItemInteractionOption option in options)
        {
            if (option.Id == optionId && option.Available)
            {
                return option.SourceModule.OnInteract(
                    this,
                    actor,
                    GetLocalOptionId(optionId));
            }
        }

        return false;
    }

    public void CollectBackpackInteractionOptions(
        InventoryRuntime.Entry entry,
        Actor actor,
        List<BackpackInteractionOption> options)
    {
        if (Definition.BackpackInteractions == null)
        {
            return;
        }

        var local = new List<BackpackInteractionOption>();
        for (int i = 0; i < Definition.BackpackInteractions.Count; i++)
        {
            var module = Definition.BackpackInteractions[i];
            if (module == null || !module.CanShow(entry, actor))
            {
                continue;
            }

            local.Clear();
            module.CollectInteractionOptions(entry, local);
            foreach (BackpackInteractionOption option in local)
            {
                options.Add(new BackpackInteractionOption(
                    i + ":" + option.Id,
                    option.DisplayName,
                    module.CanInteract(entry, actor),
                    module,
                    option.Icon));
            }
        }
    }

    public bool ExecuteBackpackInteraction(
        InventoryRuntime.Entry entry,
        string optionId,
        Actor actor)
    {
        if (actor == null || !actor.IsServer)
        {
            return false;
        }

        var options = new List<BackpackInteractionOption>();
        CollectBackpackInteractionOptions(entry, actor, options);
        foreach (BackpackInteractionOption option in options)
        {
            if (option.Id == optionId && option.Available)
            {
                return option.SourceModule.OnInteract(
                    entry,
                    actor,
                    GetLocalOptionId(optionId));
            }
        }

        return false;
    }

    private static string GetLocalOptionId(string optionId)
    {
        int separator = optionId.IndexOf(':');
        return separator >= 0 ? optionId.Substring(separator + 1) : optionId;
    }
}
