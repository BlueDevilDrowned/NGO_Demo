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
    [Header("Equipment")]
    public WeaponEquipmentData weaponEquipmentData=WeaponEquipmentData.NoWeapon();
}
