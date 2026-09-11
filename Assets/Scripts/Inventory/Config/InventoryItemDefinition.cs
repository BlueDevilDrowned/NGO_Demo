using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Item Definition", fileName = "ItemDefinition")]
public class InventoryItemDefinition : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;
    [SerializeReference] public List<ItemModule> Modules = new List<ItemModule> { new StorageShapeModule() };
    [SerializeReference] public List<ItemModuleInfo> ModuleInfos = new List<ItemModuleInfo>();

    private Dictionary<Type, ItemModuleInfo> infoByType;

    public T GetInfo<T>() where T : ItemModuleInfo
    {
        EnsureInfoIndex();
        return infoByType.TryGetValue(typeof(T), out ItemModuleInfo info)
            ? info as T
            : null;
    }

    public bool TryGetInfo<T>(out T info) where T : ItemModuleInfo
    {
        info = GetInfo<T>();
        return info != null;
    }

    public ItemModuleInfo GetInfo(Type type)
    {
        if (type == null || !typeof(ItemModuleInfo).IsAssignableFrom(type)) return null;
        EnsureInfoIndex();
        return infoByType.TryGetValue(type, out ItemModuleInfo info) ? info : null;
    }

    public void RebuildInfoIndex()
    {
        infoByType = new Dictionary<Type, ItemModuleInfo>();
        if (ModuleInfos == null) return;

        foreach (ItemModuleInfo info in ModuleInfos)
        {
            if (info == null) continue;
            info.Bind(this);
            if (!infoByType.TryAdd(info.GetType(), info))
                Debug.LogError($"{name} contains duplicate module info: {info.GetType().Name}.", this);
        }
    }

    public void SynchronizeModuleInfos()
    {
        if (Modules == null) Modules = new List<ItemModule>();
        if (ModuleInfos == null) ModuleInfos = new List<ItemModuleInfo>();

        var existing = new Dictionary<Type, ItemModuleInfo>();
        foreach (ItemModuleInfo info in ModuleInfos)
        {
            if (info != null && !existing.ContainsKey(info.GetType()))
                existing.Add(info.GetType(), info);
        }

        var synchronized = new List<ItemModuleInfo>();
        foreach (ItemModule module in Modules)
        {
            Type infoType = module?.InfoType;
            if (infoType == null || !typeof(ItemModuleInfo).IsAssignableFrom(infoType)) continue;
            if (!existing.TryGetValue(infoType, out ItemModuleInfo info))
                info = Activator.CreateInstance(infoType) as ItemModuleInfo;
            if (info != null) synchronized.Add(info);
        }

        ModuleInfos = synchronized;
        RebuildInfoIndex();
    }

    private void EnsureInfoIndex()
    {
        if (infoByType == null) RebuildInfoIndex();
    }

    private void OnEnable() => RebuildInfoIndex();

    protected virtual void OnValidate()
    {
        if (Modules == null) Modules = new List<ItemModule>();
        if (ModuleInfos == null) ModuleInfos = new List<ItemModuleInfo>();
        SynchronizeModuleInfos();
        RebuildInfoIndex();
    }
}
