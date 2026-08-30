using UnityEngine;

public sealed class WeaponRigController : MonoBehaviour
{
    [SerializeField]private Transform firstPersonWeaponMount;
    [SerializeField]private Transform thirdPersonWeaponMount;

    private WeaponInstance firstPersonWeapon;
    private WeaponInstance thirdPersonWeapon;

    public Transform FirstPersonWeaponMount=>firstPersonWeaponMount;
    public Transform ThirdPersonWeaponMount=>thirdPersonWeaponMount;
    public WeaponInstance FirstPersonWeapon=>firstPersonWeapon;
    public WeaponInstance ThirdPersonWeapon=>thirdPersonWeapon;

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
        CameraPerspectiveMode perspective)
    {
        bool showFirstPerson=
            isOwner&&perspective==CameraPerspectiveMode.FirstPerson;

        if(firstPersonWeapon!=null)
            firstPersonWeapon.gameObject.SetActive(showFirstPerson);
        if(thirdPersonWeapon!=null)
            thirdPersonWeapon.gameObject.SetActive(!showFirstPerson);
    }

    public void Unbind()
    {
        firstPersonWeapon=null;
        thirdPersonWeapon=null;
    }
}
