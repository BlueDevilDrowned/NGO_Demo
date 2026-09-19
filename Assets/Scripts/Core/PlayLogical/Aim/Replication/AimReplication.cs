using UnityEngine;

public class AimReplication:IActorSystem
{
    public Actor actor;
    public AimIntentChannel intent;
    public AimStateChannel state;
    public AimModeStateChannel modeState;
    private AimModeStateSnapshot authoritativeMode;
    private bool modeStateDirty;
    private AimTargetStateSnapshot pendingTarget;
    private bool hasPendingTarget;
    public AimReplication(Actor actor)
    {
        this.actor=actor;
        intent=new(actor);
        intent.Register();

        state=new(actor,this);
        state.Register();

        modeState=new(actor,this);
        modeState.Register();
        modeStateDirty=actor.IsServer;
        if(actor.IsServer)
            modeState.MarkDirty();
    }

    internal bool TryBuildModeState(out AimModeStateSnapshot snapshot)
    {
        snapshot=authoritativeMode;
        if(!modeStateDirty)return false;

        modeStateDirty=false;
        return true;
    }

    internal void ReceiveModeState(in AimModeStateSnapshot snapshot)
    {
        authoritativeMode=snapshot;
        actor.simulation.aimData.IsAiming=snapshot.IsAiming;
        if(actor.IsOwner)
            actor.aimSystem.data.IsAiming=snapshot.IsAiming;
    }

    internal void MarkModeDirty()
    {
        if(!actor.IsServer)return;

        authoritativeMode.IsAiming=actor.simulation.aimData.IsAiming;
        modeStateDirty=true;
        modeState.MarkDirty();
    }

    internal void ReceiveTargetState(in AimTargetStateSnapshot snapshot)
    {
        pendingTarget=snapshot;
        hasPendingTarget=true;
    }

    public bool TryConsumeTargetState(out AimTargetStateSnapshot snapshot)
    {
        snapshot=pendingTarget;
        if(!hasPendingTarget)
            return false;

        hasPendingTarget=false;
        return true;
    }

    public bool TrySampleTargetState(uint localTick,out AimTargetStateSnapshot snapshot)
    {
        return actor.actorSyncSystem.TrySamplePresentation(
            state,
            localTick,
            out snapshot);
    }
    
    bool isDisposed=false;
    public void Dispose()
    {
        if(isDisposed)return;
        isDisposed=true;
        intent.Unregister();
        state.Unregister();
        modeState.Unregister();
    }
}
