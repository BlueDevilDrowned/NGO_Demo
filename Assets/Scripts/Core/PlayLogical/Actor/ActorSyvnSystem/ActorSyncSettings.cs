using UnityEngine;

[DisallowMultipleComponent]
public sealed class ActorSyncSettings : MonoBehaviour
{
    [SerializeField] private ActorSyncConfigSO config;

    public static ActorSyncConfigSO Current{get;private set;}

    private void Awake()
    {
        if(config==null)
            Debug.LogWarning("ActorSyncSettings has no ActorSyncConfigSO; using default sync timeline settings.",this);

        if(Current!=null)
        {
            Debug.LogError("Only one ActorSyncSettings component can be active.",this);
            enabled=false;
            return;
        }

        Current=config;
        ActorSyncRuntime.Initialize(config);
    }

    private void OnDestroy()
    {
        if(Current==config)
        {
            Current=null;
            ActorSyncRuntime.Reset();
        }
    }
}
