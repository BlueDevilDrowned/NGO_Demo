using UnityEngine;

[CreateAssetMenu(fileName = "MovementSO", menuName = "Scriptable Objects/MovementSO")]
public class MovementSO : ScriptableObject
{
    public float MinDeltaMove;
    public float MinDeltaRotation;
}
