using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

[RequireComponent(typeof(AnimancerComponent))]
public class AnimancerFacade : AnimationFacadeBase
{
    private readonly Dictionary<int,EndCallbackBinding> _endBindings=new();
    private AnimancerComponent _animancer;

    public override float CurrentTime=>GetLayerTime(0);

    public override float CurrentNormalizedTime=>GetLayerNormalizedTime(0);

    public override void Initialize()
    {
        _animancer=GetComponent<AnimancerComponent>();
        if(_animancer!=null)
            _animancer.Graph.SetKeepChildrenConnected(true);
    }

    public override float GetLayerTime(int layerIndex)
    {
        AnimancerState state=GetCurrentState(layerIndex);
        return state?.Time??0f;
    }

    public override float GetLayerNormalizedTime(int layerIndex)
    {
        AnimancerState state=GetCurrentState(layerIndex);
        return state?.NormalizedTime??0f;
    }

    public override void AddCallback(
        float normalizedTime,
        Action callback,
        int layerIndex=0)
    {
        AnimancerState state=GetCurrentState(layerIndex);
        if(state==null||callback==null)return;

        state.Events(this).Add(normalizedTime,callback);
    }

    public override void ClearOnEndCallBack(int layerIndex=0)
    {
        if(!_endBindings.TryGetValue(layerIndex,out EndCallbackBinding binding))
            return;

        if(binding.State!=null)
            binding.State.Events(this).OnEnd=null;

        _endBindings.Remove(layerIndex);
    }

    public override void PlayClip(AnimationClip clip,AnimPlayOptions options)
    {
        if(clip==null)return;

        AnimancerLayer layer=GetLayer(options.Layer);
        if(layer==null)return;

        ClearOnEndCallBack(options.Layer);
        AnimancerState state=options.FadeDuration>=0f
            ?layer.Play(clip,options.FadeDuration)
            :layer.Play(clip);
        ApplyOptions(state,options);
    }

    public override void PlayTransition(
        object transitionObject,
        AnimPlayOptions options)
    {
        if(!(transitionObject is ITransition transition))
        {
            Debug.LogError(
                $"{transitionObject} does not implement Animancer.ITransition.",
                this);
            return;
        }

        AnimancerLayer layer=GetLayer(options.Layer);
        if(layer==null)return;

        ClearOnEndCallBack(options.Layer);
        AnimancerState state=options.FadeDuration>=0f
            ?layer.Play(transition,options.FadeDuration)
            :layer.Play(transition);
        ApplyOptions(state,options);
    }

    public override void PrepareTransition(object transitionObject)
    {
        if(!(transitionObject is ITransition transition))
        {
            Debug.LogError(
                $"{transitionObject} does not implement Animancer.ITransition.",
                this);
            return;
        }

        if(!TryGetAnimancer(out AnimancerComponent animancer))return;
        animancer.States.GetOrCreate(transition);
    }

    public override void SetMixerParameter(
        Vector2 parameter,
        int layerIndex=0)
    {
        AnimancerState state=GetCurrentState(layerIndex);
        if(state is MixerState<Vector2> mixer2D)
            mixer2D.Parameter=parameter;
        else if(state is MixerState<float> mixer1D)
            mixer1D.Parameter=parameter.x;
    }

    public override void SetLayerWeight(
        int layerIndex,
        float weight,
        float fadeDuration=0f)
    {
        AnimancerLayer layer=GetLayer(layerIndex);
        if(layer==null)return;

        weight=Mathf.Clamp01(weight);
        if(fadeDuration>0f)
            layer.StartFade(weight,fadeDuration);
        else
            layer.Weight=weight;
    }

    public override void SetLayerMask(int layerIndex,AvatarMask mask)
    {
        AnimancerLayer layer=GetLayer(layerIndex);
        if(layer!=null)
            layer.Mask=mask;
    }

    public override void SetOnEndCallback(
        Action callback,
        int layerIndex=0)
    {
        ClearOnEndCallBack(layerIndex);

        AnimancerState state=GetCurrentState(layerIndex);
        if(state==null||callback==null)return;

        _endBindings[layerIndex]=new EndCallbackBinding
        {
            State=state,
            Callback=callback,
        };
        state.Events(this).OnEnd=()=>HandleAnimationEnd(layerIndex);
    }

    private void OnDisable()
    {
        foreach(EndCallbackBinding binding in _endBindings.Values)
        {
            if(binding.State!=null)
                binding.State.Events(this).OnEnd=null;
        }

        _endBindings.Clear();
    }

    private bool TryGetAnimancer(out AnimancerComponent animancer)
    {
        if(_animancer==null)
            Initialize();

        animancer=_animancer;
        if(animancer!=null)return true;

        Debug.LogError(
            $"{nameof(AnimancerFacade)} requires an {nameof(AnimancerComponent)}.",
            this);
        return false;
    }

    private AnimancerLayer GetLayer(int layerIndex)
    {
        if(layerIndex<0)
        {
            Debug.LogError("Animancer layer index cannot be negative.",this);
            return null;
        }

        return TryGetAnimancer(out AnimancerComponent animancer)
            ?animancer.Layers[layerIndex]
            :null;
    }

    private AnimancerState GetCurrentState(int layerIndex)
    {
        AnimancerLayer layer=GetLayer(layerIndex);
        return layer?.CurrentState;
    }

    private static void ApplyOptions(AnimancerState state,AnimPlayOptions options)
    {
        if(state==null)return;

        state.Speed=options.Speed;
        if(options.NormalizedTime>=0f)
            state.NormalizedTime=options.NormalizedTime;
    }

    private void HandleAnimationEnd(int layerIndex)
    {
        if(!_endBindings.TryGetValue(layerIndex,out EndCallbackBinding binding))
            return;

        Action callback=binding.Callback;
        ClearOnEndCallBack(layerIndex);
        callback?.Invoke();
    }
}
