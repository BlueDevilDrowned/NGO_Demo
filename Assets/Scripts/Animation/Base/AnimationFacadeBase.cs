using System;
using UnityEngine;

public abstract class AnimationFacadeBase : MonoBehaviour, IAnimationFacade
{
    public abstract float CurrentTime{get;}

    public abstract float CurrentNormalizedTime{get;}

    public abstract void AddCallback(float normalizedTime, Action callback);
    public abstract void ClearOnEndCallBack();

    public abstract void PlayClip(AnimationClip clip, AnimPlayOptions options);

    public abstract void PlayTransition(object transition, AnimPlayOptions optons);

    public abstract void SetMixerParameter(Vector2 parameter);

    public abstract void SetOnEndCallback(Action callback);
}
