using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class AnimationArbiter : IAnimationFacade
{
    private readonly Animator animator;
    private readonly IAnimationFacade animation;
    private readonly Dictionary<object,AnimationControlRequest> controlRequests=new();
    private bool animatorDisabled;

    public AnimationArbiter(Actor actor,IAnimationFacade animation)
    {
        if(actor==null)
            throw new ArgumentNullException(nameof(actor));

        this.animation=animation??
            throw new ArgumentNullException(nameof(animation));
        animator=actor.GetComponentInChildren<Animator>(true);
        ResolveControlRequests();
    }

    public bool IsAnimatorEnabled=>!animatorDisabled;
    public bool CanExecuteCommands=>!animatorDisabled;
    public float CurrentTime=>animation.CurrentTime;
    public float CurrentNormalizedTime=>animation.CurrentNormalizedTime;

    public float GetLayerTime(int layerIndex)
    {
        return animation.GetLayerTime(layerIndex);
    }

    public float GetLayerNormalizedTime(int layerIndex)
    {
        return animation.GetLayerNormalizedTime(layerIndex);
    }

    public void Initialize()
    {
        animation.Initialize();
    }

    public void PlayClip(AnimationClip clip,AnimPlayOptions options)
    {
        if(CanExecuteCommands)
            animation.PlayClip(clip,options);
    }

    public void PlayTransition(object transition,AnimPlayOptions options)
    {
        if(CanExecuteCommands)
            animation.PlayTransition(transition,options);
    }

    public void PrepareTransition(object transition,int layerIndex=0)
    {
        if(CanExecuteCommands)
            animation.PrepareTransition(transition,layerIndex);
    }

    public void SetMixerParameter(Vector2 parameter,int layerIndex=0)
    {
        if(CanExecuteCommands)
            animation.SetMixerParameter(parameter,layerIndex);
    }

    public void SetLayerWeight(
        int layerIndex,
        float weight,
        float fadeDuration=0f)
    {
        if(CanExecuteCommands)
            animation.SetLayerWeight(layerIndex,weight,fadeDuration);
    }

    public void SetLayerAdditive(int layerIndex,bool isAdditive)
    {
        if(CanExecuteCommands)
            animation.SetLayerAdditive(layerIndex,isAdditive);
    }

    public void StopLayer(int layerIndex)
    {
        if(CanExecuteCommands)
            animation.StopLayer(layerIndex);
    }

    public void SetLayerMask(int layerIndex,AvatarMask mask)
    {
        if(CanExecuteCommands)
            animation.SetLayerMask(layerIndex,mask);
    }

    public void SetOnEndCallback(Action callback,int layerIndex=0)
    {
        if(CanExecuteCommands)
            animation.SetOnEndCallback(callback,layerIndex);
    }

    public void AddCallback(
        float normalizedTime,
        Action callback,
        int layerIndex=0)
    {
        if(CanExecuteCommands)
            animation.AddCallback(normalizedTime,callback,layerIndex);
    }

    public void ClearOnEndCallBack(int layerIndex=0)
    {
        animation.ClearOnEndCallBack(layerIndex);
    }

    public void SubmitControlRequest(
        object requester,
        in AnimationControlRequest request)
    {
        if(requester==null)
            throw new ArgumentNullException(nameof(requester));

        controlRequests[requester]=request;
        ResolveControlRequests();
    }

    public bool RemoveControlRequest(object requester)
    {
        if(requester==null)return false;

        bool removed=controlRequests.Remove(requester);
        if(removed)
            ResolveControlRequests();
        return removed;
    }

    public void Tick()
    {
        ResolveControlRequests();
    }

    private void ResolveControlRequests()
    {
        bool disableAnimator=false;
        foreach(AnimationControlRequest request in controlRequests.Values)
            disableAnimator|=request.DisableAnimator;

        animatorDisabled=disableAnimator;
        if(animator!=null&&animator.enabled==animatorDisabled)
            animator.enabled=!animatorDisabled;
    }
}
