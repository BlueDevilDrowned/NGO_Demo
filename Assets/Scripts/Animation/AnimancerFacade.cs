using System;
using Animancer;
using UnityEngine;
[RequireComponent(typeof(AnimancerComponent))]
public class AnimancerFacade : AnimationFacadeBase
{   
    private AnimancerComponent _animancer;
    private AnimancerState _currentState;
    private AnimancerState _endCallbackState;
    private EndCallbackBinding _endBinding;

    private bool _isRootMotionOverride;
    private bool _prevopisApplyRootMotion;

    private Action _OnEndCallback;
    public override float CurrentTime => _currentState?.Time??0f;

    public override float CurrentNormalizedTime => _currentState?.NormalizedTime??0f;

    private void Awake()
    {
        _animancer=GetComponent<AnimancerComponent>();
    }
    private void OnDisable()
    {
        ClearOnEndCallBack();
        _currentState=null;
    }
    public override void AddCallback(float normalizedTime, Action callback)
    {
        if(_currentState==null||callback==null)return;
        _currentState.Events(this).Add(normalizedTime,callback);
    }

    public override void ClearOnEndCallBack()
    {
        if(_endCallbackState!=null)
        {
            _endCallbackState.Events(this).OnEnd=null;
        }

        _endCallbackState=null;
        _OnEndCallback=null;
    }

    public override void PlayClip(AnimationClip clip, AnimPlayOptions options)
    {
        if(clip==null)return;
        ClearOnEndCallBack();
        _currentState=options.FadeDuration>=0?_animancer.Play(clip,options.FadeDuration):_animancer.Play(clip);
        ApplyOptions(_currentState,options);
    }

    public override void PlayTransition(object transitionObject, AnimPlayOptions options)
    {
        var transition =transitionObject as ITransition;
        if(transition==null)
        {
            Debug.LogError(
                $"{transitionObject} 没有实现 Animancer.ITransition。",
                this);

            return;
        }
        ClearOnEndCallBack();
        _currentState=options.FadeDuration>=0
            ?_animancer.Play(transition,options.FadeDuration)
            :_animancer.Play(transition);
        ApplyOptions(_currentState,options);
    }

    public override void SetMixerParameter(Vector2 parameter)
    {
        if(_currentState is MixerState<Vector2> mixer2D)
        {
            mixer2D.Parameter=parameter;
        }
        else if(_currentState is MixerState<float>mixer1D)
        {
            mixer1D.Parameter=parameter.x;
        }
    }

    public override void SetOnEndCallback(Action callback)
    {
        ClearOnEndCallBack();
        if(_currentState==null||callback==null)return;
        _endCallbackState=_currentState;
        _OnEndCallback=callback;
        _endCallbackState.Events(this).OnEnd=HandleAnimationEnd;
    }
    private static void ApplyOptions(AnimancerState state,AnimPlayOptions options)
    {
        if(state==null)return;
        state.Speed=options.Speed;
        if(options.NormalizedTime>=0f)state.NormalizedTime=options.NormalizedTime;
    }
    private void HandleAnimationEnd()
    {
        Action callback = _OnEndCallback;
        ClearOnEndCallBack();
        callback?.Invoke();
    }
}
