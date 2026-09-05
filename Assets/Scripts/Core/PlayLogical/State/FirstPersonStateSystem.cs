using System;

public sealed class FirstPersonStateSystem : IActorOwnershipSystem
{
    private const int BaseAnimationLayer=0;
    private const int FireAnimationLayer=1;
    private readonly Actor actor;
    private ActorBrainSo brain;
    private FirstPersonGlobalTransitionResolver globalTransitionResolver;
    private bool isConfigured;
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
        if(isConfigured)return;

        this.brain=brain??throw new ArgumentNullException(nameof(brain));
        isConfigured=true;
        if(actor.IsOwner)
            EnsureInitializedAndActivate();
    }

    public void PresentationUpdate(float deltaTime)
    {
        if(isDisposed||!isActive||!actor.IsOwner)return;

        BaseState target=globalTransitionResolver?.SelectNextState(
            Machine.CurrentState);
        if(target!=null)
            Machine.ChangeState(target);

        Machine.PresentationUpdate(deltaTime);
        Machine.CheckPresentationEnd();
    }

    public void OnGainedOwnership()
    {
        if(isConfigured)
            EnsureInitializedAndActivate();
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

    private void EnsureInitializedAndActivate()
    {
        if(isDisposed)return;

        if(!isInitialized)
        {
            Registry.Initialize(brain,actor);
            globalTransitionResolver=new FirstPersonGlobalTransitionResolver(
                brain,
                Registry);
            isInitialized=true;
        }

        Activate();
    }

    private void Deactivate()
    {
        if(!isActive)return;

        isActive=false;
        Machine.Stop();
        StopWeaponAnimationLayers();
    }

    private void OnWeaponChanged(WeaponInstance _)
    {
        if(!isActive)return;

        // A weapon change invalidates every animation started by the old weapon.
        // Stop both layers before reading the new weapon's animation config.
        StopWeaponAnimationLayers();

        if(Registry.TryGetState(
               FirstPersonStateType.GetWeapon,
               out FirstPersonActorState getWeaponState))
        {
            if(ReferenceEquals(Machine.CurrentState,getWeaponState))
                Machine.ReenterCurrentState();
            else
                Machine.ChangeState(getWeaponState);
            return;
        }

        Machine.ReenterCurrentState();
    }

    private void StopWeaponAnimationLayers()
    {
        IAnimationFacade facade=actor.firstPersonAnimationFacade;
        if(facade==null)return;

        facade.ClearOnEndCallBack(BaseAnimationLayer);
        facade.StopLayer(BaseAnimationLayer);
        facade.SetLayerWeight(BaseAnimationLayer,1f);

        facade.ClearOnEndCallBack(FireAnimationLayer);
        facade.StopLayer(FireAnimationLayer);
        facade.SetLayerWeight(FireAnimationLayer,0f);
    }
}
