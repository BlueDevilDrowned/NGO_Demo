using Animancer;

public sealed class UpperBodyFireState : UpperBodyState
{
    private bool hasActiveShot;
    private uint shotEndTick;

    public UpperBodyFireState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        hasActiveShot=false;
        TryFire();
    }

    public override void ServerTick()
    {
        bool isAttackHeld=
            actor.simulation.inputData.IsHeld(InputButtons.InputAttack);
        if(isAttackHeld&&TryFire())return;

        if(hasActiveShot)
        {
            if(TickTime.CurrentServerTick<shotEndTick)return;

            ReturnToWait();
            return;
        }

        if(!isAttackHeld)
            ReturnToWait();
    }

    public override void PresentationUpdate(float deltaTime)
    {
        if(!actor.IsClient)return;

        while(actor.weapon.TryConsumeShotPresentation(out ShotData shot))
        {
            PlayFireAnimation(in shot);
            //音效
            if(actor.weaponEquipment==null||actor.weaponEquipment.CurrentDefinition==null)return;
            actor.audioSystem.PlayOneShot(
                actor.weaponEquipment.CurrentDefinition.FireAudio);
        }
    }

    private void PlayFireAnimation(in ShotData shot)
    {
        WeaponAnimationSO animationConfig=
            actor.weaponEquipment?.CurrentDefinition?.animationConfig;
        if(animationConfig==null)return;

        bool aiming=actor.aimSystem?.IsAiming==true;
        ThirdPersonWeaponCombatAnimations thirdPerson=
            animationConfig.ThirdPersonUpperBody?.Combat;
        TransitionAsset thirdPersonTransition=aiming
            ?thirdPerson?.AimAttack??thirdPerson?.Attack
            :thirdPerson?.Attack;
        PlayTimedTransition(
            animation,
            thirdPersonTransition,
            Layer,
            in shot,
            HandleFireAnimationEnd);

        if(!actor.IsOwner||actor.firstPersonAnimationFacade==null)return;

        FirstPersonWeaponCombatAnimations firstPerson=
            animationConfig.FirstPerson?.Combat;
        TransitionAsset firstPersonTransition=aiming
            ?firstPerson?.AimAttack??firstPerson?.Attack
            :firstPerson?.Attack;
        PlayTimedTransition(
            actor.firstPersonAnimationFacade,
            firstPersonTransition,
            Layer,
            in shot,
            HandleFirstPersonFireAnimationEnd);
    }

    private static void PlayTimedTransition(
        IAnimationFacade target,
        TransitionAsset transitionAsset,
        int layer,
        in ShotData shot,
        System.Action onEnd)
    {
        if(target==null||transitionAsset==null)return;

        float intervalSeconds=shot.FireIntervalTicks/
                              (float)TickTime.TickRate;
        ITransition transition=transitionAsset;
        float animationLength=transition.MaximumLength;
        float animationSpeed=intervalSeconds>UnityEngine.Mathf.Epsilon&&
                             animationLength>UnityEngine.Mathf.Epsilon&&
                             !float.IsInfinity(animationLength)&&
                             !float.IsNaN(animationLength)
            ?animationLength/intervalSeconds
            :1f;

        AnimPlayOptions options=AnimPlayOptions.Default;
        options.Layer=layer;
        options.FadeDuration=0f;
        options.NormalizedTime=0f;
        options.Speed=animationSpeed;
        target.PlayTransition(transitionAsset,options);
        target.SetLayerWeight(layer,1f,0.1f);
        target.SetOnEndCallback(onEnd,layer);
    }

    public override void Exit()
    {
        animation.ClearOnEndCallBack(Layer);
        actor.firstPersonAnimationFacade?.ClearOnEndCallBack(Layer);
        actor.firstPersonAnimationFacade?.SetLayerWeight(Layer,0f,0.1f);
    }

    private void HandleFireAnimationEnd()
    {
        if(actor.IsServer)
        {
            ReturnToWait();
            return;
        }

        animation.SetLayerWeight(Layer,0f,0.1f);
    }

    private void HandleFirstPersonFireAnimationEnd()
    {
        actor.firstPersonAnimationFacade?.SetLayerWeight(Layer,0f,0.1f);
    }

    private void ReturnToWait()
    {
        stateMachine.ChangeState(
            stateRegistry.GetState<UpperBodyWaitState>());
    }

    private bool TryFire()
    {
        if(!actor.weapon.TryFire())return false;

        hasActiveShot=true;
        shotEndTick=TickTime.CurrentServerTick+
                    actor.weapon.LastShot.FireIntervalTicks;
        return true;
    }
}
