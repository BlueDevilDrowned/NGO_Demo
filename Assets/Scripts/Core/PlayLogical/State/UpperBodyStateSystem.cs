using System;

public sealed class UpperBodyStateSystem : IActorSystem
{
    private readonly Actor actor;
    private readonly UpperBodyStateReplication replication;
    private bool isInitialized;
    private bool hasCapturedState;
    private UpperBodyStateType capturedStateType;
    private uint stateEnterTick;
    private bool isProne;
    private UpperBodyActionRequest pendingActions;
    private bool hasAppliedState;
    private uint appliedStateEnterTick;

    public UpperBodyStateMachine Machine{get;}
    public UpperBodyStateRegistry Registry{get;}
    internal bool IsProne=>isProne;

    public UpperBodyStateSystem(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        Machine=new UpperBodyStateMachine();
        Registry=new UpperBodyStateRegistry();
        replication=new UpperBodyStateReplication(actor);
        actor.RegisterSystem(this);
        if(actor.weaponEquipment!=null)
            actor.weaponEquipment.WeaponChanged+=OnWeaponChanged;
    }

    public void Initialize()
    {
        if(isInitialized)return;

        Registry.Initialize(actor);
        Machine.Initialize(Registry.InitialState);

        isInitialized=true;
        CaptureAuthoritativeState(0);
    }

    public void ServerTick(uint tick)
    {
        if(!actor.IsServer)return;

        Machine.ServerTick();
        CaptureAuthoritativeState(tick);
    }

    public void PresentationUpdate(float deltaTime)
    {
        if(replication.TryConsumeState(out UpperBodyStateSnapshot snapshot)&&
           Registry.TryGetState(snapshot.StateType,out UpperBodyState state))
        {
            bool isReentry=hasAppliedState&&
                           snapshot.StateEnterTick!=appliedStateEnterTick&&
                           ReferenceEquals(Machine.CurrentState,state);
            if(isReentry)
                Machine.ReenterCurrentState();
            else
                Machine.ChangeState(state);

            hasAppliedState=true;
            appliedStateEnterTick=snapshot.StateEnterTick;
        }

        Machine.PresentationUpdate(deltaTime);
    }

    public bool RequestGetWeapon()
    {
        if(!actor.IsServer||!isInitialized)return false;

        pendingActions|=UpperBodyActionRequest.GetWeapon;
        return true;
    }

    public bool RequestChangeClip()
    {
        if(!actor.IsServer||!isInitialized)return false;

        pendingActions|=UpperBodyActionRequest.ChangeClip;
        return true;
    }

    public bool SetProne(bool prone)
    {
        if(!actor.IsServer||!isInitialized)return false;
        if(isProne==prone)return true;

        isProne=prone;
        return true;
    }

    internal bool ChangeStateFromStateLogic(UpperBodyStateType stateType)
    {
        if(!actor.IsServer||!isInitialized||
           !Registry.TryGetState(stateType,out UpperBodyState state))
            return false;

        if(ReferenceEquals(Machine.CurrentState,state))
        {
            Machine.ReenterCurrentState();
            hasCapturedState=false;
            return true;
        }

        Machine.ChangeState(state);
        return true;
    }

    internal bool ConsumeAction(UpperBodyActionRequest action)
    {
        if((pendingActions&action)!=action)return false;

        pendingActions&=~action;
        return true;
    }

    private void CaptureAuthoritativeState(uint tick)
    {
        if(!actor.IsServer||Machine.CurrentState==null)return;

        UpperBodyStateType stateType=Machine.CurrentState.StateType;
        if(hasCapturedState&&stateType==capturedStateType)return;

        hasCapturedState=true;
        capturedStateType=stateType;
        stateEnterTick=tick;
        replication.MarkAuthoritativeState(new UpperBodyStateSnapshot
        {
            StateType=stateType,
            StateEnterTick=stateEnterTick,
        });
    }

    public void Dispose()
    {
        if(actor.weaponEquipment!=null)
            actor.weaponEquipment.WeaponChanged-=OnWeaponChanged;
        replication.Dispose();
    }

    private void OnWeaponChanged(WeaponInstance weapon)
    {
        if(!isInitialized)return;

        if(actor.IsServer&&weapon!=null)
            RequestGetWeapon();
        else
        {
            if(actor.IsServer)
                pendingActions=UpperBodyActionRequest.None;
            Machine.ReenterCurrentState();
        }
    }
}
