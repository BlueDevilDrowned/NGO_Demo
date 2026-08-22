using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;

/// <summary>
/// AnimancerFacade 类是对 AnimancerComponent 的封装，提供了更高级的动画控制接口。
/// 继承自 AnimationFacadeBase，实现了一套统一的动画操作方法。
/// </summary>
[RequireComponent(typeof(AnimancerComponent))]
public class AnimancerFacade : AnimationFacadeBase
{
    /// <summary>
    /// 存储动画结束回调的字典，键为图层索引，值为结束回调绑定对象。
    /// </summary>
    private readonly Dictionary<int,EndCallbackBinding> _endBindings=new();
    private readonly Dictionary<int,float> _layerWeightTargets=new();
    /// <summary>
    /// AnimancerComponent 组件的引用，用于底层动画操作。
    /// </summary>
    private AnimancerComponent _animancer;

    /// <summary>
    /// 获取当前动画时间（基于第0层）。
    /// </summary>
    public override float CurrentTime=>GetLayerTime(0);

    /// <summary>
    /// 获取当前动画标准化时间（基于第0层）。
    /// </summary>
    public override float CurrentNormalizedTime=>GetLayerNormalizedTime(0);

    /// <summary>
    /// 初始化方法，获取 AnimancerComponent 组件并设置其属性。
    /// </summary>
    public override void Initialize()
    {
        _animancer=GetComponent<AnimancerComponent>();
        _layerWeightTargets.Clear();
        if(_animancer!=null)
            _animancer.Graph.SetKeepChildrenConnected(true);
    }

    /// <summary>
    /// 获取指定图层的当前动画时间。
    /// </summary>
    /// <param name="layerIndex">图层索引</param>
    /// <returns>当前动画时间，如果状态不存在则返回0</returns>
    public override float GetLayerTime(int layerIndex)
    {
        AnimancerState state=GetCurrentState(layerIndex);
        return state?.Time??0f;
    }

    /// <summary>
    /// 获取指定图层的当前动画标准化时间。
    /// </summary>
    /// <param name="layerIndex">图层索引</param>
    /// <returns>当前动画标准化时间，如果状态不存在则返回0</returns>
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
        public override void PlayTransition(
        object transitionObject)
    {
        if(!(transitionObject is ITransition transition))
        {
            Debug.LogError(
                $"{transitionObject} does not implement Animancer.ITransition.",
                this);
            return;
        }
        AnimPlayOptions options=AnimPlayOptions.Default;
        AnimancerLayer layer=GetLayer(options.Layer);
        if(layer==null)return;

        ClearOnEndCallBack(options.Layer);
        AnimancerState state=options.FadeDuration>=0f
            ?layer.Play(transition,options.FadeDuration)
            :layer.Play(transition);
        ApplyOptions(state,options);
    }

    public override void PrepareTransition(object transitionObject,int layerIndex=0)
    {
        if(!(transitionObject is ITransition transition))
        {
            Debug.LogError(
                $"{transitionObject} does not implement Animancer.ITransition.",
                this);
            return;
        }

        AnimancerLayer layer=GetLayer(layerIndex);
        if(layer==null)return;

        AnimancerState state=layer.Play(transition,0f);
        if(state!=null)
            state.IsPlaying=false;
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
        if(_layerWeightTargets.TryGetValue(layerIndex,out float targetWeight)&&
           Mathf.Approximately(targetWeight,weight))return;

        _layerWeightTargets[layerIndex]=weight;

        if(fadeDuration>0f)
            layer.StartFade(weight,fadeDuration);
        else
            layer.Weight=weight;
    }

    public override void StopLayer(int layerIndex)
    {
        AnimancerLayer layer=GetLayer(layerIndex);
        layer?.Stop();
    }

    public override void SetLayerAdditive(int layerIndex,bool isAdditive)
    {
        AnimancerLayer layer=GetLayer(layerIndex);
        if(layer!=null)
            layer.IsAdditive=isAdditive;
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
        _layerWeightTargets.Clear();
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
