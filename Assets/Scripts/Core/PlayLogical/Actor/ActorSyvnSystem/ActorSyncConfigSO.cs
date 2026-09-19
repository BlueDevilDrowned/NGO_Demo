using UnityEngine;

[CreateAssetMenu(
    fileName = "ActorSyncConfig",
    menuName = "Network/Actor Sync Config")]
public sealed class ActorSyncConfigSO : ScriptableObject
{
    [SerializeField, Min(0)] private int presentationDelayTicks=2;
    [SerializeField, Min(1)] private int historyTicks=32;
    [SerializeField, Min(1)] private int bufferSafetyTicks=2;

    public int PresentationDelayTicks=>Mathf.Max(0,presentationDelayTicks);
    public int HistoryTicks=>Mathf.Max(1,historyTicks);
    public int BufferSafetyTicks=>Mathf.Max(1,bufferSafetyTicks);
    public int BufferCapacityTicks=>PresentationDelayTicks+HistoryTicks+BufferSafetyTicks;
}
