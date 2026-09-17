using UnityEngine;

public sealed class WeaponRigController : MonoBehaviour
{
    [SerializeField]private Transform firstPersonWeaponMount;
    [SerializeField]private Transform thirdPersonWeaponMount;
    [Header("Logical Aim")]
    [SerializeField]private Transform logicalRotationPoint;
    [SerializeField]private Vector3 logicalRotationPointUpAxis=Vector3.up;

    private WeaponInstance firstPersonWeapon;
    private WeaponInstance thirdPersonWeapon;

    public Transform FirstPersonWeaponMount=>firstPersonWeaponMount;
    public Transform ThirdPersonWeaponMount=>thirdPersonWeaponMount;
    public WeaponInstance FirstPersonWeapon=>firstPersonWeapon;
    public WeaponInstance ThirdPersonWeapon=>thirdPersonWeapon;
    public Transform LogicalRotationPoint=>logicalRotationPoint;
    public Vector3 LogicalRotationPointUpAxis=>
        logicalRotationPointUpAxis.sqrMagnitude>0.000001f
            ?logicalRotationPointUpAxis.normalized
            :Vector3.up;

    private void OnValidate()
    {
        if(logicalRotationPointUpAxis.sqrMagnitude<=0.000001f||
           !IsFinite(logicalRotationPointUpAxis))
            logicalRotationPointUpAxis=Vector3.up;
        else
            logicalRotationPointUpAxis.Normalize();
    }

    public bool Bind(
        WeaponInstance thirdPerson,
        WeaponInstance firstPerson)
    {
        if(thirdPerson==null||!thirdPerson.IsValid()||
           firstPerson!=null&&!firstPerson.IsValid())
            return false;

        thirdPersonWeapon=thirdPerson;
        firstPersonWeapon=firstPerson;
        return true;
    }

    public bool BindFirstPerson(WeaponInstance weapon)
    {
        if(weapon==null||!weapon.IsValid())return false;

        firstPersonWeapon=weapon;
        return true;
    }

    public WeaponInstance DetachFirstPerson()
    {
        WeaponInstance detached=firstPersonWeapon;
        firstPersonWeapon=null;
        return detached;
    }

    public void SetPresentationMode(
        bool isOwner,
        CameraPerspectiveMode _)
    {
        if(firstPersonWeapon!=null)
            firstPersonWeapon.gameObject.SetActive(isOwner);
        if(thirdPersonWeapon!=null)
            thirdPersonWeapon.gameObject.SetActive(true);
    }

    public void Unbind()
    {
        firstPersonWeapon=null;
        thirdPersonWeapon=null;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&
               !float.IsNaN(value.z)&&!float.IsInfinity(value.z);
    }
}
