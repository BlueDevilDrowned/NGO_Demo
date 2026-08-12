using UnityEngine;

public sealed class WeaponInstance : MonoBehaviour
{
    [SerializeField]private Transform muzzle;
    [SerializeField]private Transform rightHandGrip;
    [SerializeField]private Transform leftHandGrip;

    public WeaponSO Definition{get;private set;}
    public Transform Muzzle=>muzzle;
    public Transform RightHandGrip=>rightHandGrip;
    public Transform LeftHandGrip=>leftHandGrip;

    internal void Initialize(WeaponSO definition)
    {
        Definition=definition;
    }

    public bool IsValid()
    {
        return muzzle!=null&&rightHandGrip!=null&&leftHandGrip!=null;
    }

    private void OnValidate()
    {
        if(muzzle==null)
            muzzle=transform.Find("Muzzle");
        if(rightHandGrip==null)
            rightHandGrip=transform.Find("RightHandGrip");
        if(leftHandGrip==null)
            leftHandGrip=transform.Find("LeftHandGrip");
    }
}
