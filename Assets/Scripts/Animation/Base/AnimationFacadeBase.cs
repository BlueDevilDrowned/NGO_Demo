using System;
using UnityEngine;

/// <summary>
/// 动画外观基类，继承自MonoBehaviour并实现IAnimationFacade接口
/// 提供了动画控制的基本抽象方法和属性
/// </summary>
public abstract class AnimationFacadeBase : MonoBehaviour, IAnimationFacade
{
    /// <summary>
    /// 获取当前动画时间
    /// </summary>
    public abstract float CurrentTime{get;}

    /// <summary>
    /// 获取当前动画标准化时间（0-1之间）
    /// </summary>
    public abstract float CurrentNormalizedTime{get;}

    /// <summary>
    /// 获取指定层级的时间
    /// </summary>
    /// <param name="layerIndex">层级索引</param>
    /// <returns>指定层级的时间</returns>
    public abstract float GetLayerTime(int layerIndex);

    /// <summary>
    /// 获取指定层级的标准化时间（0-1之间）
    /// </summary>
    /// <param name="layerIndex">层级索引</param>
    /// <returns>指定层级的标准化时间</returns>
    public abstract float GetLayerNormalizedTime(int layerIndex);

    /// <summary>
    /// 初始化动画系统
    /// </summary>
    public abstract void Initialize();

    public abstract void AddCallback(float normalizedTime, Action callback,int layerIndex=0);
    public abstract void ClearOnEndCallBack(int layerIndex=0);

    public abstract void PlayClip(AnimationClip clip, AnimPlayOptions options);

    public abstract void PlayTransition(object transition, AnimPlayOptions optons);

    public abstract void PrepareTransition(object transition);

    public abstract void SetMixerParameter(Vector2 parameter,int layerIndex=0);

    public abstract void SetLayerWeight(int layerIndex,float weight,float fadeDuration=0f);

    public abstract void SetLayerMask(int layerIndex,AvatarMask mask);

    public abstract void SetOnEndCallback(Action callback,int layerIndex=0);
}
