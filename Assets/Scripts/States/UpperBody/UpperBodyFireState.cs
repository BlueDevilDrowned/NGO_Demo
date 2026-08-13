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
            actor.runTimeData.Input.IsHeld(InputButtons.InputAttack);
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
            actor.actorAudio.PlayOneShot(actor.weaponEquipment.CurrentDefinition.FireAudio);
        }
    }

    private void PlayFireAnimation(in ShotData shot)
    {
        TransitionAsset fireTransition=actor.animancerData?.Fire;
        if(fireTransition==null)return;

        float intervalSeconds=shot.FireIntervalTicks/
                              (float)TickTime.TickRate;
        ITransition transition=fireTransition;
        float animationLength=transition.MaximumLength;
        float animationSpeed=intervalSeconds>UnityEngine.Mathf.Epsilon&&
                             animationLength>UnityEngine.Mathf.Epsilon&&
                             !float.IsInfinity(animationLength)&&
                             !float.IsNaN(animationLength)
            ?animationLength/intervalSeconds
            :1f;

        AnimPlayOptions options=AnimPlayOptions.Default;
        options.Layer=Layer;
        options.FadeDuration=0f;
        options.NormalizedTime=0f;
        options.Speed=animationSpeed;
        animation.PlayTransition(fireTransition,options);
        animation.SetLayerWeight(Layer,1f,0.1f);
        animation.SetOnEndCallback(HandleFireAnimationEnd,Layer);
    }

    public override void Exit()
    {
        animation.ClearOnEndCallBack(Layer);
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
