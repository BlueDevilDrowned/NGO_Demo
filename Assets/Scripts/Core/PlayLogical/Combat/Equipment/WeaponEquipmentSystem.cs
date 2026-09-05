using System;
using UnityEngine;

/// <summary>Creates and presents the weapon selected by the inventory.</summary>
public sealed class WeaponEquipmentSystem : IActorOwnershipSystem
{
    private readonly Actor actor;
    private readonly WeaponInventorySystem inventory;
    private ushort equippedWeaponId;
    private bool isDisposed;

    public WeaponInstance FirstPersonWeapon=>actor.weaponRig.FirstPersonWeapon;
    public WeaponInstance ThirdPersonWeapon=>actor.weaponRig.ThirdPersonWeapon;
    public Transform FirstPersonMuzzle=>FirstPersonWeapon?.Muzzle;
    public Transform Muzzle=>ThirdPersonWeapon?.Muzzle;
    public ushort CurrentWeaponId=>inventory.CurrentWeaponId;
    public WeaponSO CurrentDefinition=>CurrentWeaponId>0
        ?WeaponCatalog.Get(CurrentWeaponId)
        :null;
    public event Action<WeaponInstance> WeaponChanged;

    public WeaponEquipmentSystem(Actor actor,WeaponInventorySystem inventory)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        this.inventory=inventory??throw new ArgumentNullException(nameof(inventory));
        if(actor.weaponRig==null)
            throw new InvalidOperationException("Actor requires a weapon rig.");

        actor.RegisterSystem(this);
        inventory.Changed+=ApplyActiveWeapon;
        if(actor.perspectiveSystem!=null)
            actor.perspectiveSystem.PresentationModeChanged+=OnPresentationModeChanged;
        ApplyActiveWeapon();
    }

    public void OnGainedOwnership()
    {
        EnsureFirstPersonWeapon();
        RefreshWeaponVisibility();
    }

    public void OnLostOwnership()
    {
        actor.viewVisibilityController?.SetDynamicThirdPersonHiddenRoot(null);
        DestroyWeapon(actor.weaponRig.DetachFirstPerson());
        actor.weaponRig.SetPresentationMode(false,CameraPerspectiveMode.ThirdPerson);
    }

    public void Dispose()
    {
        if(isDisposed)return;
        isDisposed=true;
        inventory.Changed-=ApplyActiveWeapon;
        if(actor.perspectiveSystem!=null)
            actor.perspectiveSystem.PresentationModeChanged-=OnPresentationModeChanged;
        ClearWeapon();
        WeaponChanged=null;
    }

    private void ApplyActiveWeapon()
    {
        if(isDisposed)return;
        ushort weaponId=inventory.CurrentWeaponId;
        if(weaponId==0)
        {
            ClearWeapon();
            return;
        }
        if(equippedWeaponId==weaponId&&
           ThirdPersonWeapon!=null&&
           (!actor.IsOwner||FirstPersonWeapon!=null))
        {
            RefreshWeaponVisibility();
            return;
        }
        if(!WeaponCatalog.TryGet(weaponId,out WeaponSO definition)||
           definition.ThirdPersonPrefab==null||
           actor.weaponRig.ThirdPersonWeaponMount==null)
        {
            ClearWeapon();
            return;
        }
        WeaponInstance firstPrefab=definition.FirstPersonPrefab;
        if(actor.IsOwner&&(firstPrefab==null||actor.weaponRig.FirstPersonWeaponMount==null))
        {
            ClearWeapon();
            return;
        }

        WeaponInstance oldFirst=FirstPersonWeapon;
        WeaponInstance oldThird=ThirdPersonWeapon;
        WeaponInstance third=UnityEngine.Object.Instantiate(
            definition.ThirdPersonPrefab,actor.weaponRig.ThirdPersonWeaponMount,false);
        WeaponInstance first=actor.IsOwner
            ?UnityEngine.Object.Instantiate(firstPrefab,actor.weaponRig.FirstPersonWeaponMount,false)
            :null;
        if(!third.IsValid()||first!=null&&!first.IsValid()||!actor.weaponRig.Bind(third,first))
        {
            DestroyWeapon(first);
            DestroyWeapon(third);
            return;
        }
        actor.viewVisibilityController?.SetDynamicFirstPersonHiddenRoot(third.transform);
        actor.viewVisibilityController?.SetDynamicThirdPersonHiddenRoot(first?.transform);
        DestroyWeapon(oldFirst);
        DestroyWeapon(oldThird);
        equippedWeaponId=weaponId;
        RefreshWeaponVisibility();
        WeaponChanged?.Invoke(third);
    }

    private void ClearWeapon()
    {
        WeaponInstance first=FirstPersonWeapon;
        WeaponInstance third=ThirdPersonWeapon;
        bool hadWeapon=first!=null||third!=null;
        actor.viewVisibilityController?.SetDynamicFirstPersonHiddenRoot(null);
        actor.viewVisibilityController?.SetDynamicThirdPersonHiddenRoot(null);
        actor.weaponRig.Unbind();
        equippedWeaponId=0;
        DestroyWeapon(first);
        DestroyWeapon(third);
        if(hadWeapon)WeaponChanged?.Invoke(null);
    }

    private bool EnsureFirstPersonWeapon()
    {
        if(!actor.IsOwner||FirstPersonWeapon!=null||CurrentWeaponId==0)return true;
        WeaponInstance prefab=CurrentDefinition?.FirstPersonPrefab;
        Transform mount=actor.weaponRig.FirstPersonWeaponMount;
        if(prefab==null||mount==null)return false;
        WeaponInstance instance=UnityEngine.Object.Instantiate(prefab,mount,false);
        if(!instance.IsValid()||!actor.weaponRig.BindFirstPerson(instance))
        {
            DestroyWeapon(instance);
            return false;
        }
        actor.viewVisibilityController?.SetDynamicThirdPersonHiddenRoot(instance.transform);
        return true;
    }

    private void OnPresentationModeChanged(CameraPerspectiveMode _)=>RefreshWeaponVisibility();

    private void RefreshWeaponVisibility()
    {
        actor.weaponRig.SetPresentationMode(
            actor.IsOwner,
            actor.perspectiveSystem?.PresentationMode??CameraPerspectiveMode.ThirdPerson);
    }

    private static void DestroyWeapon(WeaponInstance weapon)
    {
        if(weapon!=null)UnityEngine.Object.Destroy(weapon.gameObject);
    }
}
