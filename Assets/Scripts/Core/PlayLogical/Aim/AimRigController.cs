using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AimRigController : MonoBehaviour
{
    [SerializeField]private Transform aimTarget;
    [SerializeField]private Rig aimRig;
    public Transform AimTarget=>aimTarget;

    private void Awake()
    {
        SetWeight(0f);
    }

    public void SetWeight(float weight)
    {
        if(aimRig!=null)
            aimRig.weight=Mathf.Clamp01(weight);
    }

    public void SetTargetPosition(Vector3 position)
    {
        if(aimTarget==null)return;
        aimTarget.position=position;
    }
}
