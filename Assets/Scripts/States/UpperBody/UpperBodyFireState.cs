using Animancer;

public sealed class UpperBodyFireState : UpperBodyState
{
    public UpperBodyFireState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        animation.SetLayerWeight(Layer,1f);
    }

    public override void ServerTick()
    {
        if(!actor.runTimeData.Input.IsHeld(InputButtons.InputAttack))
        {
            stateMachine.ChangeState(
                stateRegistry.GetState<UpperBodyEmptyState>());
            return;
        }

        actor.weapon.TryFire();
    }

    public override void PresentationUpdate(float deltaTime)
    {
        if(!actor.IsClient)return;

        while(actor.weapon.TryConsumeShotPresentation(out ShotData shot))
        {
            PlayFireAnimation(in shot);
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
        options.NormalizedTime=0f;
        options.Speed=animationSpeed;
        animation.PlayTransition(fireTransition,options);
    }
}
