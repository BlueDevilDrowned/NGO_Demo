using UnityEngine;

[CreateAssetMenu(fileName = "AnimationSO", menuName = "Scriptable Objects/AnimationSO")]
public class AnimationSO : ScriptableObject
{
    public float Walk_Loop_SmoothFactor=10;
    [Min(0f)]
    [Tooltip("Maximum first-person idle body turn speed in degrees per second.")]
    public float firstPersonIdleTurnAngle=360;
}
