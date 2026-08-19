//负责存放各个系统的服务器权威数据
//客户端表现层只读
//服务器逻辑计算可读可修改
using UnityEngine;

public sealed class ActorSimulationState
{
    [Header("Input")]
    public ActorInputData inputData;
    [Header("Camera")]
    public ActorCameraData cameraData;
    [Header("Aim")]
    public AimData aimData;
    [Header("Locomotion")]
    public LocomotionData locomotionData;
    public ActorStateData stateData;
    [Header("Health")]
    public float currentHealth=float.MaxValue;
    public float maxHealth=float.MaxValue;
    [Header("Equipment")]
    public WeaponEquipmentData weaponEquipmentData=WeaponEquipmentData.NoWeapon();
    [Header("State")]
    public ActorStateSnapshot actorState;
    public UpperBodyStateSnapshot upperBodyState;

    public bool WantMove=>
        locomotionData.DesiredWorldMoveDirection.sqrMagnitude>0.0001f;
    public bool WantAim=>inputData.IsHeld(InputButtons.InputAim);
}
