using UnityEngine;
using UnityEngine.Animations.Rigging;

[DefaultExecutionOrder(-100)]
public sealed class WeaponRigController : MonoBehaviour
{
    [SerializeField]private Transform weaponMount;
    [SerializeField]private Transform rightHandTarget;
    [SerializeField]private Transform leftHandTarget;

    [SerializeField]private MultiParentConstraint weaponFollow;
    [SerializeField]private TwoBoneIKConstraint rightHandIK;
    [SerializeField]private TwoBoneIKConstraint leftHandIK;
    [SerializeField,Min(0)]private int handSourceIndex;
    [SerializeField,Min(0)]private int aimSourceIndex=1;

    private WeaponInstance currentWeapon;
    private float handIKWeight;

    public Transform WeaponMount=>weaponMount;
    public WeaponInstance CurrentWeapon=>currentWeapon;

    private void Awake()
    {
        ConfigureConstraintTargets();
        SetAimBlend(0f);
    }

    private void LateUpdate()
    {
        if(handIKWeight>0f)
            SyncHandTargets();
    }

    public bool Bind(WeaponInstance weapon)
    {
        if(weapon==null||!weapon.IsValid())return false;

        if(currentWeapon!=null&&!ReferenceEquals(currentWeapon,weapon))
            Unbind();

        currentWeapon=weapon;
        SyncHandTargets();
        return true;
    }

    public void Unbind()
    {
        SetAimBlend(0f);
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

        handIKWeight=handIKBlend;
        if(handIKWeight>0f)
            SyncHandTargets();
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

    private void SyncHandTargets()
    {
        if(currentWeapon==null||
            rightHandTarget==null&&leftHandTarget==null)
            return;

        if(rightHandTarget!=null&&currentWeapon.RightHandGrip!=null)
        {
            rightHandTarget.SetPositionAndRotation(
                currentWeapon.RightHandGrip.position,
                currentWeapon.RightHandGrip.rotation);
        }

        if(leftHandTarget!=null&&currentWeapon.LeftHandGrip!=null)
        {
            leftHandTarget.SetPositionAndRotation(
                currentWeapon.LeftHandGrip.position,
                currentWeapon.LeftHandGrip.rotation);
        }
    }
}
