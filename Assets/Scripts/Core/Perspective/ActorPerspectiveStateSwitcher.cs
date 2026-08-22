using System;
using System.Collections.Generic;

/// <summary>
/// ActorPerspectiveStateSwitcher 类用于管理角色在不同视角模式下的状态切换
/// </summary>
public sealed class ActorPerspectiveStateSwitcher
{
    // 角色大脑的脚本ableObject引用
    private readonly ActorBrainSo brain;
    // 角色状态系统引用
    private readonly ActorStateSystem stateSystem;
    // 存储所有共享状态的集合
    private readonly HashSet<ActorStateType>sharedStates=new();
    // 存储所有第三人称视角状态的集合
    private readonly HashSet<ActorStateType>thirdPersonStates=new();
    // 存储所有第一人称视角状态的集合
    private readonly HashSet<ActorStateType>firstPersonStates=new();

    /// <summary>
    /// 构造函数，初始化状态切换器
    /// </summary>
    /// <param name="brain">角色大脑脚本ableObject</param>
    /// <param name="stateSystem">角色状态系统</param>
    public ActorPerspectiveStateSwitcher(
        ActorBrainSo brain,
        ActorStateSystem stateSystem)
    {
        // 初始化角色大脑和状态系统引用
        this.brain=brain;
        this.stateSystem=stateSystem??
            throw new ArgumentNullException(nameof(stateSystem));

        // 添加各种视角下的可用状态
        AddStates(brain?.SharedStates,sharedStates);
        AddStates(brain?.ThirdPerson?.AvailableStates,thirdPersonStates);
        AddStates(brain?.FirstPerson?.AvailableStates,firstPersonStates);
    }

    /// <summary>
    /// 检查是否可以切换到目标视角模式
    /// </summary>
    /// <param name="targetMode">目标视角模式</param>
    /// <returns>是否可以切换</returns>
    public bool CanSwitchTo(CameraPerspectiveMode targetMode)
    {
        // 如果无法获取当前状态类型或当前状态是共享状态，则不能切换
        if(!TryGetCurrentStateType(out ActorStateType currentType)||
           sharedStates.Contains(currentType))
            return false;

        // 检查当前状态是否已在目标模式中，或者是否可以解析到目标状态
        return IsStateInMode(currentType,targetMode)||
               TryResolveTarget(currentType,targetMode,out _);
    }

    /// <summary>
    /// 尝试切换到目标视角模式
    /// </summary>
    /// <param name="targetMode">目标视角模式</param>
    /// <returns>是否切换成功</returns>
    public bool TrySwitchTo(CameraPerspectiveMode targetMode)
    {
        // 如果无法获取当前状态类型或当前状态是共享状态，则不能切换
        if(!TryGetCurrentStateType(out ActorStateType currentType)||
           sharedStates.Contains(currentType))
            return false;

        // 如果当前状态已在目标模式中，则直接返回成功
        if(IsStateInMode(currentType,targetMode))return true;

        // 尝试解析目标状态并设置状态
        return TryResolveTarget(currentType,targetMode,out ActorStateType targetType)&&
               stateSystem.TrySetState(targetType);
    }

    /// <summary>
    /// 尝试获取当前状态类型
    /// </summary>
    /// <param name="stateType">输出参数，获取到的状态类型</param>
    /// <returns>是否成功获取状态类型</returns>
    private bool TryGetCurrentStateType(out ActorStateType stateType)
    {
        stateType=default;
        // 检查当前状态是否是ActorBaseState类型，并尝试获取其状态类型
        return stateSystem.Machine.CurrentState is ActorBaseState state&&
               stateSystem.Registry.TryGetStateType(state,out stateType);
    }

    private bool TryResolveTarget(
        ActorStateType currentType,
        CameraPerspectiveMode targetMode,
        out ActorStateType targetType)
    {
        targetType=default;
        if(brain?.PerspectiveTransitions==null)return false;

        int bestPriority=int.MinValue;
        bool found=false;
        for(int i=0;i<brain.PerspectiveTransitions.Count;i++)
        {
            ActorGlobalTransitionConfig transition=brain.PerspectiveTransitions[i];
            if(transition==null||
               !IsStateInMode(transition.TargetState,targetMode)||
               transition.AllowedFromStates==null||
               !transition.AllowedFromStates.Contains(currentType)||
               transition.Priority<=bestPriority)
                continue;

            bestPriority=transition.Priority;
            targetType=transition.TargetState;
            found=true;
        }

        return found;
    }

    private bool IsStateInMode(
        ActorStateType stateType,
        CameraPerspectiveMode mode)
    {
        return mode==CameraPerspectiveMode.FirstPerson
            ?firstPersonStates.Contains(stateType)
            :thirdPersonStates.Contains(stateType);
    }

    private static void AddStates(
        List<ActorStateConfig>configs,
        HashSet<ActorStateType>target)
    {
        if(configs==null)return;
        for(int i=0;i<configs.Count;i++)
        {
            ActorStateConfig config=configs[i];
            if(config!=null)
                target.Add(config.StateType);
        }
    }
}
