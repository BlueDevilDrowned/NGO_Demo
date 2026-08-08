using System;
using UnityEngine;

public abstract class AnimationFacadeBase : MonoBehaviour, IAnimationFacade
{
    public abstract float CurrentTime{get;}

    public abstract float CurrentNormalizedTime{get;}

    public abstract float GetLayerTime(int layerIndex);

    public abstract float GetLayerNormalizedTime(int layerIndex);

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
