using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(-100)]
public sealed class WeaponRigController : MonoBehaviour
{
    [SerializeField]private Transform weaponMount;
    [SerializeField]private Transform handIKRoot;
    [SerializeField]private Transform rightHandTarget;
    [SerializeField]private Transform leftHandTarget;

    [SerializeField]private MultiParentConstraint weaponFollow;
    [SerializeField]private TwoBoneIKConstraint rightHandIK;
    [SerializeField]private TwoBoneIKConstraint leftHandIK;
    [SerializeField,Min(0)]private int handSourceIndex;
    [SerializeField,Min(0)]private int aimSourceIndex=1;

    private WeaponInstance currentWeapon;

    private Transform rightHandOriginalParent;
    private Transform leftHandOriginalParent;
    private Vector3 rightHandOriginalLocalPosition;
    private Vector3 leftHandOriginalLocalPosition;
    private Quaternion rightHandOriginalLocalRotation;
    private Quaternion leftHandOriginalLocalRotation;

    public Transform WeaponMount=>weaponMount;
    public WeaponInstance CurrentWeapon=>currentWeapon;

    private void Awake()
    {
        CaptureTargetRestPose();
        ConfigureConstraintTargets();
        SetAimBlend(0f);
    }

    public bool Bind(WeaponInstance weapon)
    {
        if(weapon==null||!weapon.IsValid())return false;

        if(currentWeapon!=null&&!ReferenceEquals(currentWeapon,weapon))
            Unbind();

        currentWeapon=weapon;
        AttachTarget(rightHandTarget,weapon.RightHandGrip);
        AttachTarget(leftHandTarget,weapon.LeftHandGrip);
        return true;
    }

    public void Unbind()
    {
        SetAimBlend(0f);
        RestoreTarget(
            rightHandTarget,
            rightHandOriginalParent,
            rightHandOriginalLocalPosition,
            rightHandOriginalLocalRotation);
        RestoreTarget(
            leftHandTarget,
            leftHandOriginalParent,
            leftHandOriginalLocalPosition,
            leftHandOriginalLocalRotation);
        currentWeapon=null;
    }

    public void SetAimBlend(float blend)
    {
        blend=Mathf.Clamp01(blend);
        float weaponBlend=Mathf.Clamp01(blend*2f);
        float handIKBlend=Mathf.Clamp01(blend*2f-1f);

        if(weaponFollow!=null)
        {
            WeightedTransformArray sources=weaponFollow.data.sourceObjects;
            if(handSourceIndex<sources.Count&&aimSourceIndex<sources.Count)
            {
                sources.SetWeight(handSourceIndex,1f-weaponBlend);
                sources.SetWeight(aimSourceIndex,weaponBlend);
                weaponFollow.data.sourceObjects=sources;
            }
        }

        if(rightHandIK!=null)
            rightHandIK.weight=handIKBlend;
        if(leftHandIK!=null)
            leftHandIK.weight=handIKBlend;
    }

    private void CaptureTargetRestPose()
    {
        if(rightHandTarget!=null)
        {
            rightHandOriginalParent=rightHandTarget.parent;
            rightHandOriginalLocalPosition=rightHandTarget.localPosition;
            rightHandOriginalLocalRotation=rightHandTarget.localRotation;
        }

        if(leftHandTarget!=null)
        {
            leftHandOriginalParent=leftHandTarget.parent;
            leftHandOriginalLocalPosition=leftHandTarget.localPosition;
            leftHandOriginalLocalRotation=leftHandTarget.localRotation;
        }
    }

    private void ConfigureConstraintTargets()
    {
        if(rightHandIK!=null&&rightHandTarget!=null)
        {
            TwoBoneIKConstraintData data=rightHandIK.data;
            data.target=rightHandTarget;
            rightHandIK.data=data;
        }

        if(leftHandIK!=null&&leftHandTarget!=null)
        {
            TwoBoneIKConstraintData data=leftHandIK.data;
            data.target=leftHandTarget;
            leftHandIK.data=data;
        }
    }

    private static void AttachTarget(Transform target,Transform grip)
    {
        if(target==null||grip==null)return;
        target.SetParent(grip,false);
        target.localPosition=Vector3.zero;
        target.localRotation=Quaternion.identity;
    }

    private void RestoreTarget(
        Transform target,
        Transform originalParent,
        Vector3 localPosition,
        Quaternion localRotation)
    {
        if(target==null)return;
        target.SetParent(originalParent!=null?originalParent:handIKRoot,false);
        target.localPosition=localPosition;
        target.localRotation=localRotation;
    }
}
