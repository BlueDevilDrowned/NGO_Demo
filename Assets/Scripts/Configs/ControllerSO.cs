using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ControllerSO", menuName = "Scriptable Objects/ControllerSO")]
public class ControllerSO : ScriptableObject
{
    [Min(0f)]public float WalkSpeed=2f;
    public float WalkmaxRotation=180;
    [FormerlySerializedAs("JogSpeed")]
    [Min(0f)]public float RunSpeed=3f;
    public float JogmaxRotation=270;
    [Min(0f)]public float SprintSpeed=5f;
    public float AimWalkSpeed=3;

    public float JumpVelocity=10f;
    public float JumpMaxRotation=180;
    [Tooltip("跳跃水平速度")]
    public float JumpSpeed=3f;
    [Header("Gravite")]
    public float Gravite=-20;
    public float GroundedVelocity=-1;
    public float UpFactor=0.5f;
    public float FallFactor=2f;
    public float HoldFactor=0.1f;
    public float MaxfallSpeed=-20f;
    public float HoldSpeed=0.5f;

    [Header("Weapon Drop")]
    [Min(0f)]public float WeaponDropThrowSpeed=1.5f;
    [Min(0f)]public float WeaponDropUpOffset=0.5f;
    [Min(0f)]public float WeaponDropForwardOffset=0.5f;

    public Vector3 GetWeaponDropPosition(Transform actorTransform)
    {
        if(actorTransform==null)return Vector3.zero;

        return actorTransform.position+
               actorTransform.up*WeaponDropUpOffset+
               actorTransform.forward*WeaponDropForwardOffset;
    }

    public float GetMoveSpeed(LocomotionStateType state)
    {
        return state switch
        {
            LocomotionStateType.Sprint=>Mathf.Max(0f,SprintSpeed),
            LocomotionStateType.Run=>Mathf.Max(0f,RunSpeed),
            _=>Mathf.Max(0f,WalkSpeed),
        };
    }
}
