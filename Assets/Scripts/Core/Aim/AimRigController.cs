using UnityEngine;

public class AimRigController : MonoBehaviour
{
    [SerializeField]private Transform aimTarget;
    public Transform AimTarget=>aimTarget;

    public void SetTargetPosition(Vector3 position)
    {
        if(aimTarget==null)return;
        aimTarget.position=position;
    }
}
