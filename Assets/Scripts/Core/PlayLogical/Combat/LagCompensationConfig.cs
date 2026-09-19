using UnityEngine;

[CreateAssetMenu(
    fileName = "LagCompensationConfig",
    menuName = "Network/Lag Compensation Config")]
public sealed class LagCompensationConfig : ScriptableObject
{
    [SerializeField, Min(0.001f)] private float captureIntervalSeconds = 1f / 60f;
    [SerializeField, Min(0.05f)] private float historyDurationSeconds = 0.5f;
    [SerializeField] private LayerMask staticCollisionMask;
    [SerializeField] private string rewindProxyLayerName = "RewindProxy";
    [SerializeField, Min(8)] private int raycastBufferSize = 128;

    [Header("Debug Draw")]
    [SerializeField] private bool enableDebugDraw = true;
    [SerializeField, Min(0f)] private float debugDrawDurationSeconds = 5f;
    [SerializeField] private bool clearDebugOnNextShot = true;
    [SerializeField, Range(1, 128)] private int maxDebugShots = 16;
    [SerializeField] private bool drawDebugTickLabels = true;
    [SerializeField, Min(1)] private int debugHitboxSampleIntervalTicks = 3;

    public float CaptureIntervalSeconds => Mathf.Max(0.001f, captureIntervalSeconds);
    public float HistoryDurationSeconds => Mathf.Max(0.05f, historyDurationSeconds);
    public LayerMask StaticCollisionMask => staticCollisionMask;
    public string RewindProxyLayerName => rewindProxyLayerName;
    public int RaycastBufferSize => Mathf.Max(8, raycastBufferSize);
    public bool EnableDebugDraw => enableDebugDraw;
    public float DebugDrawDurationSeconds => Mathf.Max(0f, debugDrawDurationSeconds);
    public bool ClearDebugOnNextShot => clearDebugOnNextShot;
    public int MaxDebugShots => Mathf.Clamp(maxDebugShots, 1, 128);
    public bool DrawDebugTickLabels => drawDebugTickLabels;
    public int DebugHitboxSampleIntervalTicks => Mathf.Max(1, debugHitboxSampleIntervalTicks);

    public int GetCaptureIntervalTicks(uint tickRate)
    {
        return Mathf.Max(1, Mathf.RoundToInt(CaptureIntervalSeconds * tickRate));
    }

    public int GetHistoryCapacity(uint tickRate)
    {
        int intervalTicks = GetCaptureIntervalTicks(tickRate);
        int historyTicks = Mathf.CeilToInt(HistoryDurationSeconds * tickRate);
        return Mathf.Max(2, Mathf.CeilToInt(historyTicks / (float)intervalTicks) + 2);
    }
}
