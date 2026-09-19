using System;
using UnityEngine;

public sealed class LocomotionReplication : IDisposable
{
    private readonly Actor actor;
    private readonly LocomotionReplicationChannel motionChannel;
    private readonly LocomotionStateChannel stateChannel;
    private LocomotionMotionSnapshot motionState;
    private LocomotionStateSnapshot state;
    private bool motionStateDirty;
    private bool stateDirty;
    private bool hasPendingMotionState;
    private bool hasPendingState;
    private bool isDisposed;

    public LocomotionReplication(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        motionChannel=new LocomotionReplicationChannel(actor,this);
        motionChannel.Register();
        stateChannel=new LocomotionStateChannel(actor,this);
        stateChannel.Register();
        if(actor.IsServer)
        {
            actor.NetworkManager.OnClientConnectedCallback+=OnClientConnected;
            motionStateDirty=true;
            stateDirty=true;
            motionChannel.MarkDirty();
            stateChannel.MarkDirty();
        }
    }

    public void MarkAuthoritativeState(in LocomotionData data)
    {
        if(!actor.IsServer)return;

        LocomotionMotionSnapshot nextMotion=new LocomotionMotionSnapshot
        {
            DesiredWorldMoveDirection=data.DesiredWorldMoveDirection,
            DesiredLocalMoveAngle=data.DesiredLocalMoveAngle,
        };
        LocomotionStateSnapshot nextState=new LocomotionStateSnapshot{StateType=data.stateType};
        actor.simulation.locomotionData=data;

        if(!motionState.Equals(nextMotion))
        {
            motionState=nextMotion;
            motionStateDirty=true;
            motionChannel.MarkDirty();
        }

        if(!state.Equals(nextState))
        {
            state=nextState;
            stateDirty=true;
            stateChannel.MarkDirty();
        }
    }

    internal bool TryBuildMotionState(out LocomotionMotionSnapshot snapshot)
    {
        snapshot=motionState;
        if(!motionStateDirty)return false;
        motionStateDirty=false;
        return true;
    }

    internal bool TryBuildState(out LocomotionStateSnapshot snapshot)
    {
        snapshot=state;
        if(!stateDirty)return false;
        stateDirty=false;
        return true;
    }

    internal void ReceiveMotionState(in LocomotionMotionSnapshot snapshot)
    {
        motionState=snapshot;
        actor.simulation.locomotionData.DesiredWorldMoveDirection=snapshot.DesiredWorldMoveDirection;
        actor.simulation.locomotionData.DesiredLocalMoveAngle=snapshot.DesiredLocalMoveAngle;
        hasPendingMotionState=true;
    }

    internal void ReceiveState(in LocomotionStateSnapshot snapshot)
    {
        state=snapshot;
        actor.simulation.locomotionData.stateType=snapshot.StateType;
        hasPendingState=true;
    }

    public bool TryConsumeState(
        out LocomotionMotionSnapshot motion,
        out LocomotionStateSnapshot discrete)
    {
        motion=motionState;
        discrete=state;
        bool hasPending=hasPendingMotionState||hasPendingState;

        if(hasPendingMotionState)
        {
            actor.simulation.locomotionData.DesiredWorldMoveDirection=motion.DesiredWorldMoveDirection;
            actor.simulation.locomotionData.DesiredLocalMoveAngle=motion.DesiredLocalMoveAngle;
        }

        hasPendingMotionState=false;
        hasPendingState=false;
        return hasPending;
    }

    public bool TrySampleMotionState(
        uint localTick,
        out LocomotionMotionSnapshot snapshot)
    {
        return actor.actorSyncSystem.TrySamplePresentation(
            motionChannel,
            localTick,
            out snapshot);
    }

    public void Dispose()
    {
        if(isDisposed)return;
        isDisposed=true;
        if(actor.NetworkManager!=null)
            actor.NetworkManager.OnClientConnectedCallback-=OnClientConnected;
        motionChannel.Unregister();
        stateChannel.Unregister();
    }

    private void OnClientConnected(ulong _)
    {
        motionStateDirty=true;
        stateDirty=true;
        motionChannel.MarkDirty();
        stateChannel.MarkDirty();
    }
}
