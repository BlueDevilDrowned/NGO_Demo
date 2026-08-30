using UnityEngine;

public sealed class WeaponInstance : MonoBehaviour
{
    [SerializeField]private Transform muzzle;

    public Transform Muzzle=>muzzle;
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
    }
}
