using System;
using UnityEngine;

public interface IAnimationFacade
{
    float CurrentTime{get;}
    float CurrentNormalizedTime{get;}
    void Initialize();
    void PlayClip(AnimationClip clip,AnimPlayOptions options);
    void PlayTransition(object transition,AnimPlayOptions optons);
    void SetMixerParameter(Vector2 parameter);
    void SetOnEndCallback(Action callback);
    void ClearOnEndCallBack();
    void AddCallback(float normalizedTime,Action callback);
}
