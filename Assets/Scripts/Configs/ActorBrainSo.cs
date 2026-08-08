using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActorBrainSo", menuName = "Actor/Brain")]
public class ActorBrainSo : ScriptableObject
{
    [Tooltip("第一个状态时初始状态")]
    public ActorStateType InitialState=ActorStateType.Idle;
    public List<ActorStateConfig>AvailableStates=new();

    public UpperBodyStateType InitialUpperBodyState=UpperBodyStateType.Empty;
    public List<UpperBodyStateConfig>AvailableUpperBodyStates=new()
    {
        new UpperBodyStateConfig
        {
            StateType=UpperBodyStateType.Empty,
        },
    };

    [Tooltip("全局转换配置。优先级数值越大越先判断，同优先级按列表顺序判断")]
    public List<ActorGlobalTransitionConfig>GlobalTransitions=new();
}

[Serializable]
public sealed class ActorStateConfig
{
    public ActorStateType StateType;
    public AimModePolicy AimModePolicy=AimModePolicy.ForceNormal;
}

[Serializable]
public sealed class UpperBodyStateConfig
{
    public UpperBodyStateType StateType;
}

public enum UpperBodyStateType
{
    Empty,
}

public enum AimModePolicy
{
    ForceNormal,
    ForceAiming,
    Preserve,
}

public enum ActorMode : byte
{
    Normal,
    Aiming,
}

[Serializable]
public class ActorGlobalTransitionConfig
{
    [Tooltip("满足进入条件后切换到的目标状态")]
    public ActorStateType TargetState;

    [Tooltip("优先级数值越大越先判断")]
    public int Priority;

    [Tooltip("允许通过这条全局转换进入目标状态的来源状态")]
    public List<ActorStateType>AllowedFromStates=new();
}

public enum ActorStateType
{
    Idle,
    MoveStart,
    MoveLoop,
    MoveStop,
    Jump,
    Fall,
    Land,
    AimIdle,
    AimMove,
}
