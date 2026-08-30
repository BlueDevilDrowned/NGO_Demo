using System;

public sealed class FirstPersonStateSystem : IActorOwnershipSystem
{
    private readonly Actor actor;
    private FirstPersonTransitionResolver transitionResolver;
    private bool isInitialized;
    private bool isActive;
    private bool isDisposed;

    public StateMachine Machine{get;}=new();
    public FirstPersonStateRegistry Registry{get;}=new();

    public FirstPersonStateSystem(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        actor.RegisterSystem(this);

        if(actor.weaponEquipment!=null)
            actor.weaponEquipment.WeaponChanged+=OnWeaponChanged;
    }

    public void Initialize(ActorBrainSo brain)
    {
        if(isInitialized)return;

        Registry.Initialize(brain,actor);
        transitionResolver=new FirstPersonTransitionResolver(brain,Registry);
        isInitialized=true;
        if(actor.IsOwner)
            Activate();
    }

    public void PresentationUpdate(float deltaTime)
    {
        if(isDisposed||!isActive||!actor.IsOwner)return;

        BaseState target=transitionResolver?.SelectNextState(
            Machine.CurrentState);
        if(target!=null)
            Machine.ChangeState(target);

        Machine.PresentationUpdate(deltaTime);
    }

    public void OnGainedOwnership()
    {
        if(isInitialized)
            Activate();
    }

    public void OnLostOwnership()
    {
        Deactivate();
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        if(actor.weaponEquipment!=null)
            actor.weaponEquipment.WeaponChanged-=OnWeaponChanged;
        Deactivate();
    }

    private void Activate()
    {
        if(isDisposed||isActive||Registry.InitialState==null)return;

        isActive=true;
        actor.firstPersonAnimationFacade?.Initialize();
        Machine.Initialize(Registry.InitialState);
    }

    private void Deactivate()
    {
        if(!isActive)return;

        isActive=false;
        Machine.Stop();
    }

    private void OnWeaponChanged(WeaponInstance _)
    {
        if(isActive)
            Machine.ReenterCurrentState();
    }
}
