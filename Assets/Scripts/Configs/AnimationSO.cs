using UnityEngine;

[CreateAssetMenu(fileName = "AnimationSO", menuName = "Scriptable Objects/AnimationSO")]
public class AnimationSO : ScriptableObject
{
    public float Walk_Loop_SmoothFactor=10;
    [Header("第一人称")]
    public float IdleMAxFloat=60;
}
