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
    [Tooltip("Local-space axis on Rotation Point used as the weapon's world up reference.")]
    [SerializeField]private Vector3 rotationPointUpAxis=Vector3.up;

    private WeaponInstance boundWeapon;
    private Transform cachedAimTransform;
    private Transform cachedLeftHandFollow;
    private bool leftFollowResolvedFromWeapon;
    private bool offsetsValid;
    private bool rightTargetInitialized;
    private bool leftTargetInitialized;
    private bool hasLastValidWorldAimUp;

    private Vector3 aimPositionInWeapon;
    private Quaternion aimRotationInWeapon;
    private Vector3 weaponPositionInRightHand;
    private Quaternion weaponRotationInRightHand;
    private Vector3 leftGripPositionInWeapon;
    private Quaternion leftGripRotationInWeapon;
    private Vector3 lastValidWorldAimUp;

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
        if(rotationPointUpAxis.sqrMagnitude<=DirectionEpsilon||
           !IsFinite(rotationPointUpAxis))
            rotationPointUpAxis=Vector3.up;
        else
            rotationPointUpAxis.Normalize();
    }

    private void BindWeapon(WeaponInstance weapon)
    {
        boundWeapon=weapon;
        cachedAimTransform=null;
        offsetsValid=false;
        hasLastValidWorldAimUp=false;

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
               aimDirection,
               rotationPoint,
               rotationPointUpAxis,
               weapon.AimAxis,
               weapon.AimUpAxis,
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
        if(!TryReadLeftGripOffset())
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

        if(!TryReadLeftGripOffset())
            return;

        cachedLeftHandFollow=leftHandFollow;
        InitializeTargetFromTip(leftHandIK, ref leftTargetInitialized);
    }

    private bool TryReadLeftGripOffset()
    {
        if(boundWeapon==null||leftHandFollow==null)
            return false;

        Transform weaponTransform=boundWeapon.transform;
        leftGripPositionInWeapon=
            Quaternion.Inverse(weaponTransform.rotation)*
            (leftHandFollow.position-weaponTransform.position);
        leftGripRotationInWeapon=
            Quaternion.Inverse(weaponTransform.rotation)*
            leftHandFollow.rotation;
        return IsFinite(leftGripPositionInWeapon)&&
               IsFinite(leftGripRotationInWeapon);
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

    private bool TryBuildAimRotation(
        Vector3 worldAimDirection,
        Transform worldReference,
        Vector3 localReferenceUpAxis,
        Vector3 localAimAxis,
        Vector3 localAimUpAxis,
        out Quaternion rotation)
    {
        rotation=Quaternion.identity;
        if(worldReference==null||
           worldAimDirection.sqrMagnitude<=DirectionEpsilon||
           localReferenceUpAxis.sqrMagnitude<=DirectionEpsilon||
           localAimAxis.sqrMagnitude<=DirectionEpsilon||
           localAimUpAxis.sqrMagnitude<=DirectionEpsilon||
           !IsFinite(worldAimDirection)||
           !IsFinite(localReferenceUpAxis)||!IsFinite(localAimAxis)||
           !IsFinite(localAimUpAxis))
            return false;

        Vector3 worldForward=worldAimDirection.normalized;
        Vector3 worldUpReference=worldReference.TransformDirection(
            localReferenceUpAxis.normalized);
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
                worldReference.right,
                worldForward);
        if(worldUp.sqrMagnitude<=DirectionEpsilon)
            worldUp=Vector3.ProjectOnPlane(
                worldReference.forward,
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
