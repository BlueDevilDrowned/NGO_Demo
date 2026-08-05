using UnityEngine;

[CreateAssetMenu(fileName = "ControllerSO", menuName = "Scriptable Objects/ControllerSO")]
public class ControllerSO : ScriptableObject
{
    public float WalkSpeed=2f;
    public float WalkmaxRotation=180;
    public float JogSpeed;
    public float SprintSpeed;
}
