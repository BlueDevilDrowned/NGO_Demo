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
    public ItemInstance(InventoryItemDefinition definition) : this(string.Empty, definition) { }
    public ItemInstance(string itemId, InventoryItemDefinition definition)
    { ItemId=itemId ?? string.Empty; Definition=definition ?? throw new ArgumentNullException(nameof(definition)); }
    public void CollectInteractionOptions(Actor actor, List<ItemInteractionOption> options)
    {
        if(Modules==null) return;
        var local = new List<ItemInteractionOption>();
        for(int i=0;i<Modules.Count;i++)
        {
            ItemModule module=Modules[i];
            if(module==null || !module.CanShow(Definition,actor)) continue;
            local.Clear();
            module.CollectInteractionOptions(Definition,local);
            foreach(var option in local)
                options.Add(new ItemInteractionOption(i+":"+option.Id,option.DisplayName,
                    module.CanInteract(Definition,actor),module,option.Icon));
        }
    }
    public bool ExecuteInteraction(string optionId, Actor actor)
    {
        if(actor==null || !actor.IsServer) return false;
        var options=new List<ItemInteractionOption>();
        CollectInteractionOptions(actor,options);
        foreach(var option in options)
            if(option.Id==optionId && option.Available)
                return option.SourceModule.OnInteract(this,actor,optionId.Substring(optionId.IndexOf(':')+1));
        return false;
    }
}
