using UnityEngine;

public enum WeaponModelType
{
    Shared,
    FirstPerson,
    ThirdPerson,
}

public sealed class WeaponInstance : MonoBehaviour
{
    [SerializeField]private Transform muzzle;
    [Header("Model")]
    [SerializeField]private WeaponModelType modelType=WeaponModelType.Shared;
    [Header("Aim")]
    [Tooltip("Reference transform for the weapon's aiming axis. Its position is the aim origin.")]
    [SerializeField]private Transform aimTransform;
    [Tooltip("Local-space axis on Aim Transform that points along the weapon's aim direction.")]
    [SerializeField]private Vector3 aimAxis=Vector3.forward;
    [Tooltip("Local-space axis on Aim Transform that points toward the top of the weapon. Must not be parallel to Aim Axis.")]
    [SerializeField]private Vector3 aimUpAxis=Vector3.up;
    [Header("IK")]
    [Tooltip("Weapon-space grip followed by the character's left-hand IK.")]
    [SerializeField]private Transform leftHandGrip;

    public Transform Muzzle=>muzzle;
    public WeaponModelType ModelType=>modelType;
    public bool IncludesThirdPerson=>
        modelType==WeaponModelType.Shared||
        modelType==WeaponModelType.ThirdPerson;
    public Transform AimTransform=>aimTransform!=null?aimTransform:transform;
    public Vector3 AimAxis=>aimAxis;
    public Vector3 AimUpAxis=>aimUpAxis;
    public Transform LeftHandGrip=>leftHandGrip;

    /// <summary>
    /// Gets the weapon's aim direction in world space.
    /// </summary>
    public bool TryGetAimDirection(out Vector3 direction)
    {
        direction=Vector3.zero;
        if(!IncludesThirdPerson||
           aimAxis.sqrMagnitude<=0.000001f||
           !IsFinite(aimAxis))
            return false;

        direction=AimTransform.TransformDirection(aimAxis.normalized);
        return direction.sqrMagnitude>0.000001f&&IsFinite(direction);
    }

    /// <summary>
    ///数据是否合法
    /// </summary>
    /// <returns></returns>
    public bool IsValid()
    {
        return muzzle!=null;
    }

    private void OnValidate()
    {
        if(muzzle==null)
            muzzle=transform.Find("Muzzle");

        if(IncludesThirdPerson&&aimTransform==null)
        {
            aimTransform=transform.Find("AimPivot");
            aimTransform??=transform.Find("Aim");
        }

        if(!IncludesThirdPerson)
            return;

        if(aimAxis.sqrMagnitude<=0.000001f||!IsFinite(aimAxis))
            aimAxis=Vector3.forward;
        else
            aimAxis.Normalize();

        Vector3 projectedUp=Vector3.ProjectOnPlane(aimUpAxis,aimAxis);
        if(projectedUp.sqrMagnitude<=0.000001f||!IsFinite(projectedUp))
        {
            Vector3 fallback=Mathf.Abs(Vector3.Dot(aimAxis,Vector3.up))<0.999f
                ?Vector3.up
                :Vector3.forward;
            projectedUp=Vector3.ProjectOnPlane(fallback,aimAxis);
        }
        aimUpAxis=projectedUp.normalized;
    }

    private static bool IsFinite(Vector3 value)
    {
        return IsFinite(value.x)&&IsFinite(value.y)&&IsFinite(value.z);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
