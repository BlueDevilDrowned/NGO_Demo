using UnityEngine;

public sealed class ActorRootPoseReplication:IActorSystem
{
    private const float YawThreshold=0.01f;

    private readonly Actor actor;
    private readonly ActorRootPoseChannel stateChannel;
    private ActorRootPoseSnapshot state;
    private ActorRootPoseSnapshot receivedState;
    private bool stateDirty;
    private bool hasReceivedState;
    private bool isDisposed;

    public ActorRootPoseReplication(Actor actor,float initialYaw)
    {
        this.actor=actor;
        state=new ActorRootPoseSnapshot
        {
            Yaw=Mathf.Repeat(initialYaw,360f),
        };
        stateDirty=actor.IsServer;

        stateChannel=new ActorRootPoseChannel(actor,this);
        stateChannel.Register();
        if(actor.IsServer)
            stateChannel.MarkDirty();

        if(actor.IsServer)
            actor.NetworkManager.OnClientConnectedCallback+=OnClientConnected;
    }

    public void MarkAuthoritativeYaw(float yaw)
    {
        if(!actor.IsServer||!ActorCameraDataUtility.IsFinite(yaw))
            return;

        yaw=Mathf.Repeat(yaw,360f);
        if(Mathf.Abs(Mathf.DeltaAngle(state.Yaw,yaw))<=YawThreshold)
            return;

        state.Yaw=yaw;
        stateDirty=true;
        stateChannel.MarkDirty();
    }

    internal bool TryBuildState(out ActorRootPoseSnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;

        stateDirty=false;
        return true;
    }

    internal void ReceiveState(in ActorRootPoseSnapshot snapshot)
    {
        receivedState=snapshot;
        receivedState.Yaw=Mathf.Repeat(receivedState.Yaw,360f);
        hasReceivedState=true;
    }

    public bool TryConsumeState(out ActorRootPoseSnapshot snapshot)
    {
        snapshot=receivedState;
        if(!hasReceivedState)return false;

        hasReceivedState=false;
        return true;
    }

    public bool TrySamplePresentationState(
        uint localTick,
        out ActorRootPoseSnapshot snapshot)
    {
        return actor.actorSyncSystem.TrySamplePresentation(
            stateChannel,
            localTick,
            out snapshot);
    }

    private void OnClientConnected(ulong clientId)
    {
        stateDirty=true;
        stateChannel.MarkDirty();
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        if(actor.NetworkManager!=null)
            actor.NetworkManager.OnClientConnectedCallback-=OnClientConnected;
        stateChannel.Unregister();
    }
}
