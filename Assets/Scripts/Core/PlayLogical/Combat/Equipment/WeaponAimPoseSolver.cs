using UnityEngine;

public struct WeaponAimGeometry
{
    public Vector3 AimPositionInWeapon;
    public Quaternion AimRotationInWeapon;
    public Vector3 MuzzlePositionInWeapon;
    public Vector3 AimAxis;
    public Vector3 AimUpAxis;
}

public struct WeaponAimPose
{
    public Vector3 WeaponPosition;
    public Quaternion WeaponRotation;
    public Vector3 MuzzlePosition;
    public Vector3 AimDirection;
}

public static class WeaponAimPoseSolver
{
    private const float DirectionEpsilon=0.000001f;

    public static bool TryCaptureGeometry(
        WeaponInstance weapon,
        out WeaponAimGeometry geometry)
    {
        geometry=default;
        if(weapon==null||!weapon.IncludesThirdPerson||weapon.Muzzle==null)
            return false;

        Transform weaponTransform=weapon.transform;
        Transform aimTransform=weapon.AimTransform;
        if(aimTransform==null)return false;

        Quaternion inverseWeaponRotation=
            Quaternion.Inverse(weaponTransform.rotation);
        geometry=new WeaponAimGeometry
        {
            AimPositionInWeapon=inverseWeaponRotation*
                (aimTransform.position-weaponTransform.position),
            AimRotationInWeapon=inverseWeaponRotation*aimTransform.rotation,
            MuzzlePositionInWeapon=inverseWeaponRotation*
                (weapon.Muzzle.position-weaponTransform.position),
            AimAxis=weapon.AimAxis,
            AimUpAxis=weapon.AimUpAxis,
        };
        return IsFinite(in geometry);
    }

    public static bool TrySolve(
        in WeaponAimGeometry geometry,
        Vector3 rotationPointPosition,
        Quaternion rotationPointRotation,
        Vector3 rotationPointUpAxis,
        float aimOriginDistance,
        Vector3 aimTarget,
        ref Vector3 lastValidWorldAimUp,
        ref bool hasLastValidWorldAimUp,
        out WeaponAimPose pose)
    {
        pose=default;
        Vector3 pivotToTarget=aimTarget-rotationPointPosition;
        float targetDistance=pivotToTarget.magnitude;
        aimOriginDistance=Mathf.Max(0f,aimOriginDistance);
        if(!IsFinite(targetDistance)||
           targetDistance<=aimOriginDistance+Mathf.Epsilon)
            return false;

        Vector3 aimDirection=pivotToTarget/targetDistance;
        if(!TryBuildAimRotation(
               aimDirection,
               rotationPointRotation,
               rotationPointUpAxis,
               geometry.AimAxis,
               geometry.AimUpAxis,
               ref lastValidWorldAimUp,
               ref hasLastValidWorldAimUp,
               out Quaternion desiredAimRotation))
            return false;

        Vector3 desiredAimPosition=
            rotationPointPosition+aimDirection*aimOriginDistance;
        Quaternion weaponRotation=desiredAimRotation*
                                    Quaternion.Inverse(
                                        geometry.AimRotationInWeapon);
        Vector3 weaponPosition=desiredAimPosition-
                               weaponRotation*geometry.AimPositionInWeapon;
        pose=new WeaponAimPose
        {
            WeaponPosition=weaponPosition,
            WeaponRotation=weaponRotation,
            MuzzlePosition=weaponPosition+
                           weaponRotation*geometry.MuzzlePositionInWeapon,
            AimDirection=aimDirection,
        };
        return IsFinite(in pose);
    }

    private static bool TryBuildAimRotation(
        Vector3 worldAimDirection,
        Quaternion referenceRotation,
        Vector3 localReferenceUpAxis,
        Vector3 localAimAxis,
        Vector3 localAimUpAxis,
        ref Vector3 lastValidWorldAimUp,
        ref bool hasLastValidWorldAimUp,
        out Quaternion rotation)
    {
        rotation=Quaternion.identity;
        if(worldAimDirection.sqrMagnitude<=DirectionEpsilon||
           localReferenceUpAxis.sqrMagnitude<=DirectionEpsilon||
           localAimAxis.sqrMagnitude<=DirectionEpsilon||
           localAimUpAxis.sqrMagnitude<=DirectionEpsilon||
           !IsFinite(worldAimDirection)||
           !IsFinite(localReferenceUpAxis)||
           !IsFinite(localAimAxis)||!IsFinite(localAimUpAxis)||
           !IsFinite(referenceRotation))
            return false;

        Vector3 worldForward=worldAimDirection.normalized;
        Vector3 worldUpReference=
            referenceRotation*localReferenceUpAxis.normalized;
        if(worldUpReference.sqrMagnitude<=DirectionEpsilon||
           !IsFinite(worldUpReference))
            return false;

        Vector3 worldUp=Vector3.ProjectOnPlane(
            worldUpReference,
            worldForward);
        if(worldUp.sqrMagnitude<=DirectionEpsilon&&hasLastValidWorldAimUp)
            worldUp=Vector3.ProjectOnPlane(
                lastValidWorldAimUp,
                worldForward);
        if(worldUp.sqrMagnitude<=DirectionEpsilon)
            worldUp=Vector3.ProjectOnPlane(
                referenceRotation*Vector3.right,
                worldForward);
        if(worldUp.sqrMagnitude<=DirectionEpsilon)
            worldUp=Vector3.ProjectOnPlane(
                referenceRotation*Vector3.forward,
                worldForward);
        if(worldUp.sqrMagnitude<=DirectionEpsilon||!IsFinite(worldUp))
            return false;

        Vector3 localForward=localAimAxis.normalized;
        Vector3 localUp=Vector3.ProjectOnPlane(
            localAimUpAxis,
            localForward);
        if(localUp.sqrMagnitude<=DirectionEpsilon||!IsFinite(localUp))
            return false;

        worldUp.Normalize();
        localUp.Normalize();
        Quaternion worldBasis=Quaternion.LookRotation(worldForward,worldUp);
        Quaternion localBasis=Quaternion.LookRotation(localForward,localUp);
        rotation=worldBasis*Quaternion.Inverse(localBasis);
        lastValidWorldAimUp=worldUp;
        hasLastValidWorldAimUp=true;
        return IsFinite(rotation);
    }

    private static bool IsFinite(in WeaponAimGeometry geometry)
    {
        return IsFinite(geometry.AimPositionInWeapon)&&
               IsFinite(geometry.AimRotationInWeapon)&&
               IsFinite(geometry.MuzzlePositionInWeapon)&&
               IsFinite(geometry.AimAxis)&&
               IsFinite(geometry.AimUpAxis);
    }

    private static bool IsFinite(in WeaponAimPose pose)
    {
        return IsFinite(pose.WeaponPosition)&&
               IsFinite(pose.WeaponRotation)&&
               IsFinite(pose.MuzzlePosition)&&
               IsFinite(pose.AimDirection);
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x)&&IsFinite(value.y)&&IsFinite(value.z);
    }

    private static bool IsFinite(Quaternion value)
    {
        return IsFinite(value.x)&&IsFinite(value.y)&&
               IsFinite(value.z)&&IsFinite(value.w);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
