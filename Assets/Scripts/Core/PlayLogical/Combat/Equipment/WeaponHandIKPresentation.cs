using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public sealed class WeaponHandIKPresentation : MonoBehaviour
{
    private const float DirectionEpsilon=0.000001f;

    [Header("Actor")]
    [SerializeField]private Actor actor;

    [Header("Rig")]
    [SerializeField]private TwoBoneIKConstraint rightHandIK;
    [SerializeField]private TwoBoneIKConstraint leftHandIK;
    [Tooltip("Grip transform followed by the left hand. When missing, the active third-person weapon is queried.")]
    [SerializeField]private Transform leftHandFollow;

    [Header("Aim")]
    [SerializeField]private Transform aimTarget;
    [Tooltip("Constraint point from which WeaponSO's aim-origin distance is measured.")]
    [SerializeField]private Transform rotationPoint;

    private WeaponInstance boundWeapon;
    private Transform cachedAimTransform;
    private Transform cachedLeftHandFollow;
    private bool leftFollowResolvedFromWeapon;
    private bool offsetsValid;
    private bool rightTargetInitialized;
    private bool leftTargetInitialized;

    private Vector3 aimPositionInWeapon;
    private Quaternion aimRotationInWeapon;
    private Vector3 weaponPositionInRightHand;
    private Quaternion weaponRotationInRightHand;
    private Vector3 leftGripPositionInWeapon;
    private Quaternion leftGripRotationInWeapon;

    public bool HasValidRightHandTarget{get;private set;}
    public Vector3 DesiredWeaponPosition{get;private set;}
    public Quaternion DesiredWeaponRotation{get;private set;}=Quaternion.identity;

    private void Awake()
    {
        actor??=GetComponentInParent<Actor>();
        ResetTargetsToCurrentHands();
    }

    private void Update()
    {
        ActorIKPresentationSO config=actor?.actorSO?.ikPresentationSO;
        WeaponInstance activeWeapon=
            actor?.weaponEquipment?.ThirdPersonWeapon;
        WeaponSO weaponConfig=actor?.weaponEquipment?.CurrentDefinition;

        if(activeWeapon!=boundWeapon)
            BindWeapon(activeWeapon);

        TryResolveLeftHandFollow(activeWeapon);
        if(cachedLeftHandFollow!=leftHandFollow)
            CacheLeftHandFollow();

        if(config==null||weaponConfig==null||
           !TryCalculateDesiredWeaponPose(
               activeWeapon,
               weaponConfig,
               out Vector3 desiredWeaponPosition,
               out Quaternion desiredWeaponRotation))
        {
            HasValidRightHandTarget=false;
            ApplyWeights(config, false, false);
            return;
        }

        DesiredWeaponPosition=desiredWeaponPosition;
        DesiredWeaponRotation=desiredWeaponRotation;

        Quaternion desiredRightHandRotation=
            desiredWeaponRotation*
            Quaternion.Inverse(weaponRotationInRightHand);
        Vector3 desiredRightHandPosition=
            desiredWeaponPosition-
            desiredRightHandRotation*weaponPositionInRightHand;

        Transform rightTarget=rightHandIK!=null
            ?rightHandIK.data.target
            :null;
        if(rightTarget==null)
        {
            HasValidRightHandTarget=false;
            ApplyWeights(config, false, false);
            return;
        }

        MoveTarget(
            rightTarget,
            desiredRightHandPosition,
            desiredRightHandRotation,
            config,
            ref rightTargetInitialized);
        HasValidRightHandTarget=true;

        Quaternion limitedWeaponRotation=
            rightTarget.rotation*weaponRotationInRightHand;
        Vector3 limitedWeaponPosition=
            rightTarget.position+
            rightTarget.rotation*weaponPositionInRightHand;
        bool hasLeftTarget=TryUpdateLeftHandTarget(
            limitedWeaponPosition,
            limitedWeaponRotation,
            config);
        ApplyWeights(config, true, hasLeftTarget);
    }

    private void OnDisable()
    {
        HasValidRightHandTarget=false;
        ApplyWeights(null, false, false);
    }

    private void OnValidate()
    {
        actor??=GetComponentInParent<Actor>();
    }

    private void BindWeapon(WeaponInstance weapon)
    {
        boundWeapon=weapon;
        cachedAimTransform=null;
        offsetsValid=false;

        if(leftFollowResolvedFromWeapon)
        {
            leftHandFollow=null;
            cachedLeftHandFollow=null;
            leftFollowResolvedFromWeapon=false;
        }

        TryResolveLeftHandFollow(weapon);
        CacheWeaponOffsets();
        CacheLeftHandFollow();
        ResetTargetsToCurrentHands();
    }

    private bool TryCalculateDesiredWeaponPose(
        WeaponInstance weapon,
        WeaponSO weaponConfig,
        out Vector3 desiredPosition,
        out Quaternion desiredRotation)
    {
        desiredPosition=default;
        desiredRotation=Quaternion.identity;

        if(weapon==null||!weapon.IncludesThirdPerson||
           rightHandIK==null||aimTarget==null||rotationPoint==null)
            return false;

        Transform aimTransform=weapon.AimTransform;
        if(!offsetsValid||aimTransform!=cachedAimTransform)
            CacheWeaponOffsets();
        if(!offsetsValid)
            return false;

        Vector3 pivotToTarget=aimTarget.position-rotationPoint.position;
        float targetDistance=pivotToTarget.magnitude;
        float aimOriginDistance=Mathf.Max(
            0f,
            weaponConfig.AimOriginDistanceFromRotationPoint);
        if(!IsFinite(targetDistance)||
           targetDistance<=aimOriginDistance+Mathf.Epsilon)
            return false;

        Vector3 aimDirection=pivotToTarget/targetDistance;
        if(!TryBuildAimRotation(
               aimTransform,
               aimDirection,
               weapon.AimAxis,
               out Quaternion desiredAimRotation))
            return false;

        Vector3 desiredAimPosition=
            rotationPoint.position+aimDirection*aimOriginDistance;
        desiredRotation=
            desiredAimRotation*Quaternion.Inverse(aimRotationInWeapon);
        desiredPosition=
            desiredAimPosition-desiredRotation*aimPositionInWeapon;
        return IsFinite(desiredPosition)&&IsFinite(desiredRotation);
    }

    private bool TryUpdateLeftHandTarget(
        Vector3 desiredWeaponPosition,
        Quaternion desiredWeaponRotation,
        ActorIKPresentationSO config)
    {
        if(leftHandIK==null||leftHandFollow==null||
           cachedLeftHandFollow!=leftHandFollow)
            return false;

        Transform leftTarget=leftHandIK.data.target;
        if(leftTarget==null)
            return false;

        Vector3 desiredLeftPosition=
            desiredWeaponPosition+
            desiredWeaponRotation*leftGripPositionInWeapon;
        Quaternion desiredLeftRotation=
            desiredWeaponRotation*leftGripRotationInWeapon;
        MoveTarget(
            leftTarget,
            desiredLeftPosition,
            desiredLeftRotation,
            config,
            ref leftTargetInitialized);
        return true;
    }

    private void CacheWeaponOffsets()
    {
        offsetsValid=false;
        if(boundWeapon==null||rightHandIK==null)
            return;

        Transform rightHand=rightHandIK.data.tip;
        Transform aimTransform=boundWeapon.AimTransform;
        if(rightHand==null||aimTransform==null)
            return;

        Transform weaponTransform=boundWeapon.transform;
        aimPositionInWeapon=
            Quaternion.Inverse(weaponTransform.rotation)*
            (aimTransform.position-weaponTransform.position);
        aimRotationInWeapon=
            Quaternion.Inverse(weaponTransform.rotation)*
            aimTransform.rotation;
        weaponPositionInRightHand=
            Quaternion.Inverse(rightHand.rotation)*
            (weaponTransform.position-rightHand.position);
        weaponRotationInRightHand=
            Quaternion.Inverse(rightHand.rotation)*
            weaponTransform.rotation;
        cachedAimTransform=aimTransform;
        offsetsValid=true;
    }

    private void CacheLeftHandFollow()
    {
        cachedLeftHandFollow=null;
        leftTargetInitialized=false;
        if(boundWeapon==null||leftHandFollow==null)
            return;

        Transform weaponTransform=boundWeapon.transform;
        leftGripPositionInWeapon=
            Quaternion.Inverse(weaponTransform.rotation)*
            (leftHandFollow.position-weaponTransform.position);
        leftGripRotationInWeapon=
            Quaternion.Inverse(weaponTransform.rotation)*
            leftHandFollow.rotation;
        cachedLeftHandFollow=leftHandFollow;
        InitializeTargetFromTip(leftHandIK, ref leftTargetInitialized);
    }

    private void TryResolveLeftHandFollow(WeaponInstance weapon)
    {
        if(leftHandFollow!=null||weapon==null||weapon.LeftHandGrip==null)
            return;

        leftHandFollow=weapon.LeftHandGrip;
        leftFollowResolvedFromWeapon=true;
    }

    private void ResetTargetsToCurrentHands()
    {
        rightTargetInitialized=false;
        leftTargetInitialized=false;
        InitializeTargetFromTip(rightHandIK, ref rightTargetInitialized);
        InitializeTargetFromTip(leftHandIK, ref leftTargetInitialized);
    }

    private static void InitializeTargetFromTip(
        TwoBoneIKConstraint constraint,
        ref bool initialized)
    {
        initialized=false;
        if(constraint==null)
            return;

        Transform target=constraint.data.target;
        Transform tip=constraint.data.tip;
        if(target==null||tip==null)
            return;

        target.SetPositionAndRotation(tip.position,tip.rotation);
        initialized=true;
    }

    private static void MoveTarget(
        Transform target,
        Vector3 desiredPosition,
        Quaternion desiredRotation,
        ActorIKPresentationSO config,
        ref bool initialized)
    {
        if(!initialized)
        {
            target.SetPositionAndRotation(desiredPosition,desiredRotation);
            initialized=true;
            return;
        }

        target.SetPositionAndRotation(
            Vector3.MoveTowards(
                target.position,
                desiredPosition,
                config.MaxHandPositionDeltaPerFrame),
            Quaternion.RotateTowards(
                target.rotation,
                desiredRotation,
                config.MaxHandRotationDeltaPerFrame));
    }

    private void ApplyWeights(
        ActorIKPresentationSO config,
        bool enableRight,
        bool enableLeft)
    {
        if(rightHandIK!=null)
            rightHandIK.weight=enableRight&&config!=null
                ?config.RightHandWeight
                :0f;
        if(leftHandIK!=null)
            leftHandIK.weight=enableLeft&&config!=null
                ?config.LeftHandWeight
                :0f;
    }

    private static bool TryBuildAimRotation(
        Transform aimTransform,
        Vector3 worldAimDirection,
        Vector3 localAimAxis,
        out Quaternion rotation)
    {
        rotation=Quaternion.identity;
        if(aimTransform==null||
           worldAimDirection.sqrMagnitude<=DirectionEpsilon||
           localAimAxis.sqrMagnitude<=DirectionEpsilon||
           !IsFinite(worldAimDirection)||!IsFinite(localAimAxis))
            return false;

        Vector3 currentWorldAimDirection=
            aimTransform.TransformDirection(localAimAxis.normalized);
        if(currentWorldAimDirection.sqrMagnitude<=DirectionEpsilon||
           !IsFinite(currentWorldAimDirection))
            return false;

        Quaternion directionDelta=Quaternion.FromToRotation(
            currentWorldAimDirection.normalized,
            worldAimDirection.normalized);
        rotation=directionDelta*aimTransform.rotation;
        return IsFinite(rotation);
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
