using UnityEngine;

[CreateAssetMenu(
    fileName="ActorIKPresentationSO",
    menuName="Scriptable Objects/Actor IK Presentation")]
public sealed class ActorIKPresentationSO : ScriptableObject
{
    [Header("Weights")]
    [Range(0f,1f)]public float RightHandWeight=1f;
    [Range(0f,1f)]public float LeftHandWeight=1f;

    [Header("Per Frame Limits")]
    [Tooltip("Maximum world-space distance an IK hand target may move per rendered frame.")]
    [Min(0f)]public float MaxHandPositionDeltaPerFrame=0.05f;
    [Tooltip("Maximum angle an IK hand target may rotate per rendered frame.")]
    [Range(0f,180f)]public float MaxHandRotationDeltaPerFrame=12f;

    private void OnValidate()
    {
        RightHandWeight=Mathf.Clamp01(RightHandWeight);
        LeftHandWeight=Mathf.Clamp01(LeftHandWeight);
        MaxHandPositionDeltaPerFrame=Mathf.Max(
            0f,
            MaxHandPositionDeltaPerFrame);
        MaxHandRotationDeltaPerFrame=Mathf.Clamp(
            MaxHandRotationDeltaPerFrame,
            0f,
            180f);
    }
}
