using System;

public sealed class UpperBodyStateSystem : IActorSystem
{
    private readonly Actor actor;
    private readonly UpperBodyStateReplication replication;
    private bool isInitialized;
    private bool hasCapturedState;
    private UpperBodyStateType capturedStateType;
    private uint stateEnterTick;

    public UpperBodyStateMachine Machine{get;}
    public UpperBodyStateRegistry Registry{get;}

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

    public void Initialize(ActorBrainSo brain)
    {
        if(isInitialized)return;
        if(brain==null)throw new ArgumentNullException(nameof(brain));

        Registry.Initialize(brain,actor);
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
           Registry.TryGetState(
               snapshot.StateType,
               out UpperBodyState state))
        {
            Machine.ChangeState(state);
        }

        Machine.PresentationUpdate(deltaTime);
    }

    private void CaptureAuthoritativeState(uint tick)
    {
        if(!actor.IsServer||
           !Registry.TryGetStateType(
               Machine.CurrentState,
               out UpperBodyStateType stateType))
            return;

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

    private void OnWeaponChanged(WeaponInstance _)
    {
        if(isInitialized)
            Machine.ReenterCurrentState();
    }
}
