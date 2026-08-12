using UnityEngine;

[CreateAssetMenu(fileName="InteractSO",menuName="Scriptable Objects/InteractSO")]
public sealed class InteractSO : ScriptableObject
{
    [Header("Ray Interaction")]
    [Min(0f)]public float RayShowDistance=5f;
    [Min(0f)]public float RayInteractDistance=3f;
    public LayerMask InteractRayLayer;

    private void OnValidate()
    {
        RayInteractDistance=Mathf.Max(0f,RayInteractDistance);
        RayShowDistance=Mathf.Max(RayInteractDistance,RayShowDistance);
    }
}
