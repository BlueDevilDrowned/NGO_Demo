using System;
using Animancer;
using UnityEngine;

public class UpperBodyStateMachine
{
    public const int AnimationLayer=1;

    private readonly IAnimationFacade animation;

    public UpperBodyState CurrentState{get;private set;}
    public float AnimationNormalizedTime=>
        animation.GetLayerNormalizedTime(AnimationLayer);

    private Action onEndCallback;

    public UpperBodyStateMachine(IAnimationFacade animation)
    {
        this.animation=animation??throw new ArgumentNullException(nameof(animation));
    }

    public void PlayAnimation(WeaponUpperBodyStateAnimation configuration)
    {
        TransitionAsset clip=configuration?.Clip;
        if(clip==null)
        {
            animation.SetLayerAdditive(AnimationLayer,false);
            animation.StopLayer(AnimationLayer);
            animation.SetLayerWeight(AnimationLayer,0f,0.1f);
            return;
        }

        animation.SetLayerAdditive(AnimationLayer,configuration.Additive);

        AnimPlayOptions options=AnimPlayOptions.Default;
        options.Layer=AnimationLayer;
        animation.PlayTransition(clip,options);
        ApplyAnimationWeight(configuration);
    }

    public void ApplyAnimationWeight(WeaponUpperBodyStateAnimation configuration)
    {
        animation.SetLayerWeight(
            AnimationLayer,
            Mathf.Clamp01(configuration?.GlobalWeight??0f),
            0.1f);
    }

    public void Initialize(UpperBodyState startState)
    {
        if(startState==null)
            throw new ArgumentNullException(nameof(startState));

        CurrentState=startState;
        ClearOnEndCallback();
        CurrentState.Enter();
    }

    public void ServerTick()
    {
        CurrentState?.ServerTick();
        CheckEnd();
    }

    public void PresentationUpdate(float deltaTime)
    {
        CurrentState?.PresentationUpdate(deltaTime);
        CurrentState?.ApplyParameter();
    }

    public void ChangeState(UpperBodyState next)
    {
        if(next==null||ReferenceEquals(CurrentState,next))return;
        CurrentState?.Exit();
        ClearOnEndCallback();
        CurrentState=next;
        CurrentState.Enter();
    }

    public void SetOnEndCallback(Action callback)
    {
        onEndCallback=callback;
    }

    public void ReenterCurrentState()
    {
        if(CurrentState==null)return;

        CurrentState.Exit();
        ClearOnEndCallback();
        CurrentState.Enter();
    }

    private void CheckEnd()
    {
        if(onEndCallback==null||CurrentState==null||
           CurrentState.NormalizedTime<1f)return;

        Action callback=onEndCallback;
        ClearOnEndCallback();
        callback.Invoke();
    }

    private void ClearOnEndCallback()
    {
        onEndCallback=null;
    }
}
