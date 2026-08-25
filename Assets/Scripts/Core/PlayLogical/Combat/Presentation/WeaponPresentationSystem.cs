using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>
/// 管理一个玩家的全部武器表现资源，并按WeaponId分发射击事件。
/// </summary>
public sealed class WeaponPresentationSystem : IDisposable
{
    private readonly Transform root;
    private readonly Dictionary<ushort,WeaponPresentationResources>
        resourcesByWeapon=new();
    private bool isDisposed;

    public WeaponPresentationSystem(Transform owner)
    {
        if(owner==null)throw new ArgumentNullException(nameof(owner));

        GameObject rootObject=new($"{owner.name} Weapon Presentation");
        SceneManager.MoveGameObjectToScene(rootObject,owner.gameObject.scene);
        root=rootObject.transform;
    }

    /// <summary>
    /// 提前准备指定武器的对象池。
    /// </summary>
    public bool Prepare(ushort weaponId)
    {
        return GetOrCreateResources(weaponId)!=null;
    }

    public void Apply(in ShotData shotEvent)
    {
        if(isDisposed)return;

        WeaponPresentationResources resources=
            GetOrCreateResources(shotEvent.WeaponId);
        resources?.Apply(in shotEvent);
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        foreach(WeaponPresentationResources resources in
                resourcesByWeapon.Values)
            resources.Dispose();
        resourcesByWeapon.Clear();
        Object.Destroy(root.gameObject);
    }

    private WeaponPresentationResources GetOrCreateResources(ushort weaponId)
    {
        if(isDisposed||weaponId==0)return null;
        if(resourcesByWeapon.TryGetValue(
            weaponId,
            out WeaponPresentationResources resources))return resources;
        if(!WeaponCatalog.TryGet(weaponId,out WeaponSO config))return null;

        resources=new WeaponPresentationResources(root,config);
        resourcesByWeapon.Add(weaponId,resources);
        return resources;
    }
}
