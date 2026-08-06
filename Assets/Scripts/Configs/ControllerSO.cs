using UnityEngine;

[CreateAssetMenu(fileName = "ControllerSO", menuName = "Scriptable Objects/ControllerSO")]
public class ControllerSO : ScriptableObject
{
    public float WalkSpeed=2f;
    public float WalkmaxRotation=180;
    public float JogSpeed=3f;
    public float JogmaxRotation=270;
    public float SprintSpeed;
    [Header("Gravite")]
    public float Gravite=-20;
    public float GroundedVelocity=-1;
    public float UpFactor=0.5f;
    public float FallFactor=2f;
    public float HoldFactor=0.1f;
    public float MaxfallSpeed=-20f;
    public float HoldSpeed=0.5f;
}
