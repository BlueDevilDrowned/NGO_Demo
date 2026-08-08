using System;
using UnityEngine;

public interface IAnimationFacade
{
    float CurrentTime{get;}
    float CurrentNormalizedTime{get;}
    float GetLayerTime(int layerIndex);
    float GetLayerNormalizedTime(int layerIndex);
    void Initialize();
    void PlayClip(AnimationClip clip,AnimPlayOptions options);
    void PlayTransition(object transition,AnimPlayOptions optons);
    void PrepareTransition(object transition);
    void SetMixerParameter(Vector2 parameter,int layerIndex=0);
    void SetLayerWeight(int layerIndex,float weight,float fadeDuration=0f);
    void SetLayerMask(int layerIndex,AvatarMask mask);
    void SetOnEndCallback(Action callback,int layerIndex=0);
    void ClearOnEndCallBack(int layerIndex=0);
    void AddCallback(float normalizedTime,Action callback,int layerIndex=0);
}
