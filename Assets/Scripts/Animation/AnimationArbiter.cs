using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 动画仲裁器类，负责管理多个动画控制请求，并决定最终是否启用动画器
/// </summary>
public sealed class AnimationArbiter
{
    // 动画器组件
    private readonly Animator animator;
    // 存储请求者到动画控制请求的映射字典
    private readonly Dictionary<object,AnimationControlRequest> controlRequests=new();
    // 标记动画器是否被禁用
    private bool animatorDisabled;

    /// <summary>
    /// 构造函数，初始化动画仲裁器
    /// </summary>
    /// <param name="actor">演员对象，用于获取动画器组件</param>
    public AnimationArbiter(Actor actor)
    {
        // 检查演员对象是否为空
        if(actor==null)
            throw new ArgumentNullException(nameof(actor));

        // 获取演员子对象中的动画器组件
        animator=actor.GetComponentInChildren<Animator>(true);
        ResolveControlRequests();
    }

    /// <summary>
    /// 获取动画器是否启用的状态
    /// </summary>
    public bool IsAnimatorEnabled=>!animatorDisabled;

    /// <summary>
    /// 提交动画控制请求
    /// </summary>
    /// <param name="requester">请求者对象</param>
    /// <param name="request">动画控制请求</param>
    public void SubmitControlRequest(
        object requester,
        in AnimationControlRequest request)
    {
        // 检查请求者是否为空
        if(requester==null)
            throw new ArgumentNullException(nameof(requester));

        // 存储或更新控制请求
        controlRequests[requester]=request;
        ResolveControlRequests();
    }

    /// <summary>
    /// 移除动画控制请求
    /// </summary>
    /// <param name="requester">请求者对象</param>
    /// <returns>是否成功移除请求</returns>
    public bool RemoveControlRequest(object requester)
    {
        // 检查请求者是否为空
        if(requester==null)return false;

        // 尝试移除请求
        bool removed=controlRequests.Remove(requester);
        if(removed)
            ResolveControlRequests();
        return removed;
    }

    /// <summary>
    /// 每帧更新方法，用于处理动画控制请求
    /// </summary>
    public void Tick()
    {
        ResolveControlRequests();
    }

    /// <summary>
    /// 解析所有动画控制请求，决定最终的动画器状态
    /// </summary>
    private void ResolveControlRequests()
    {
        // 检查是否有任何请求要求禁用动画器
        bool disableAnimator=false;
        foreach(AnimationControlRequest request in controlRequests.Values)
            disableAnimator|=request.DisableAnimator;

        // 更新动画器禁用状态
        animatorDisabled=disableAnimator;
        // 如果动画器存在且当前状态与目标状态不同，则更新动画器状态
        if(animator!=null&&animator.enabled==animatorDisabled)
            animator.enabled=!animatorDisabled;
    }
}
