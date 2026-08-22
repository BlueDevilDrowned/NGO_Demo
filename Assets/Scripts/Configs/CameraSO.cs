using UnityEngine;

[CreateAssetMenu(fileName = "CameraSO", menuName = "Scriptable Objects/CameraSO")]
public class CameraSO : ScriptableObject
{
    [Header("灵敏度——鼠标")]
    public float PointerYawSensitivity=0.12f;
    public float PointerPitchSensitivity=0.12f;
    [Header("每秒度数——手柄")]
    public float StickYawDegreesPerSecond=180f;
    public float StickPitchDegreesPerSecond=120f;
    [Header("Free Look Pitch Limits")]
    public float FreeLookMinPitch=-50;
    public float FreeLookMaxPitch=70;
    [Header("Aim Pitch Limits")]
    public float AimMinPitch=-40f;
    public float AimMaxPitch=50f;

    [Header("First Person Pointer Sensitivity")]
    [Min(0f)]public float FirstPersonPointerYawSensitivity=0.12f;
    [Min(0f)]public float FirstPersonPointerPitchSensitivity=0.12f;
    [Header("First Person Stick Degrees Per Second")]
    [Min(0f)]public float FirstPersonStickYawDegreesPerSecond=180f;
    [Min(0f)]public float FirstPersonStickPitchDegreesPerSecond=120f;
    [Header("First Person Yaw Limits")]
    public float FirstPersonMinYaw=-90f;
    public float FirstPersonMaxYaw=90f;
    [Header("First Person Pitch Limits")]
    public float FirstPersonMinPitch=-80f;
    public float FirstPersonMaxPitch=80f;
}
