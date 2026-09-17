using System;
using UnityEngine;

public sealed class WeaponAimPoseSystem:IActorSystem
{
    private readonly Actor actor;
    private readonly WeaponEquipmentSystem equipment;
    private readonly Transform rotationPoint;
    private readonly Vector3 rotationPointUpAxis;
    private readonly Vector3 rotationPointLocalPosition;
    private readonly Quaternion rotationPointLocalRotation;

    private WeaponInstance cachedWeapon;
    private Transform cachedAimTransform;
    private Transform cachedMuzzle;
    private WeaponAimGeometry geometry;
    private bool hasGeometry;
    private Vector3 lastValidWorldAimUp;
    private bool hasLastValidWorldAimUp;

    public bool IsConfigured=>rotationPoint!=null;

    public WeaponAimPoseSystem(
        Actor actor,
        WeaponEquipmentSystem equipment)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        this.equipment=equipment??
            throw new ArgumentNullException(nameof(equipment));

        WeaponRigController rig=actor.weaponRig;
        rotationPoint=rig?.LogicalRotationPoint;
        rotationPointUpAxis=rig!=null
            ?rig.LogicalRotationPointUpAxis
            :Vector3.up;
        if(rotationPoint!=null)
        {
            Transform root=actor.transform;
            rotationPointLocalPosition=root.InverseTransformPoint(
                rotationPoint.position);
            rotationPointLocalRotation=
                Quaternion.Inverse(root.rotation)*rotationPoint.rotation;
        }

        equipment.WeaponChanged+=OnWeaponChanged;
        actor.RegisterSystem(this);
    }

    public bool TryResolve(
        in ActorRootPose rootPose,
        Vector3 aimTarget,
        out WeaponAimPose pose)
    {
        pose=default;
        WeaponInstance weapon=equipment.ThirdPersonWeapon;
        WeaponSO definition=equipment.CurrentDefinition;
        if(rotationPoint==null||weapon==null||definition==null||
           !EnsureGeometry(weapon))
            return false;

        Vector3 pointPosition=rootPose.Position+
            rootPose.Rotation*Vector3.Scale(
                rotationPointLocalPosition,
                rootPose.Scale);
        Quaternion pointRotation=
            rootPose.Rotation*rotationPointLocalRotation;
        return WeaponAimPoseSolver.TrySolve(
            in geometry,
            pointPosition,
            pointRotation,
            rotationPointUpAxis,
            definition.AimOriginDistanceFromRotationPoint,
            aimTarget,
            ref lastValidWorldAimUp,
            ref hasLastValidWorldAimUp,
            out pose);
    }

    public void Dispose()
    {
        equipment.WeaponChanged-=OnWeaponChanged;
    }

    private bool EnsureGeometry(WeaponInstance weapon)
    {
        Transform aimTransform=weapon.AimTransform;
        Transform muzzle=weapon.Muzzle;
        if(hasGeometry&&cachedWeapon==weapon&&
           cachedAimTransform==aimTransform&&cachedMuzzle==muzzle)
            return true;

        cachedWeapon=weapon;
        cachedAimTransform=aimTransform;
        cachedMuzzle=muzzle;
        hasGeometry=WeaponAimPoseSolver.TryCaptureGeometry(
            weapon,
            out geometry);
        hasLastValidWorldAimUp=false;
        return hasGeometry;
    }

    private void OnWeaponChanged(WeaponInstance _)
    {
        cachedWeapon=null;
        cachedAimTransform=null;
        cachedMuzzle=null;
        hasGeometry=false;
        hasLastValidWorldAimUp=false;
    }
}
