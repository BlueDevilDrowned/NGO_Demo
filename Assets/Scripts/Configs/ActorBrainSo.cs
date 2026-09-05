using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "ActorBrainSo", menuName = "Actor/Brain")]
public class ActorBrainSo : ScriptableObject
{
    [Header("Initial")]
    public CameraPerspectiveMode InitialPerspectiveMode=
        CameraPerspectiveMode.ThirdPerson;

    [Header("Shared")]
    public List<ActorStateConfig>SharedStates=new();
    [Tooltip("可以应用于所有全身状态的转换")]
    public List<ActorGlobalTransitionConfig>SharedTransitions=new();

    [Header("Full Body")]
    [FormerlySerializedAs("ThirdPerson")]
    public ActorStateGraphConfig FullBody=new()
    {
        InitialState=ActorStateType.Idle,
    };

    [Header("First Person")]
    public FirstPersonStateGraphConfig FirstPerson=new()
    {
        InitialState=FirstPersonStateType.Idle,
    };

    public ActorStateType GetInitialStateType()
    {
        return FullBody!=null?FullBody.InitialState:ActorStateType.Idle;
    }
}

[Serializable]
public sealed class ActorStateGraphConfig
{
    public ActorStateType InitialState=ActorStateType.Idle;
    public List<ActorStateConfig>AvailableStates=new();
    [Tooltip("优先级数值越大越先判断，同优先级按列表顺序判断")]
    public List<ActorGlobalTransitionConfig>GlobalTransitions=new();
}

[Serializable]
public sealed class FirstPersonStateGraphConfig
{
    public FirstPersonStateType InitialState=FirstPersonStateType.Idle;
    public List<FirstPersonStateConfig>AvailableStates=new();
    [FormerlySerializedAs("Transitions")]
    [Tooltip("第一人称全局打断转换；普通状态切换由具体状态自行决定")]
    public List<FirstPersonGlobalTransitionConfig>GlobalTransitions=new();
}

[Serializable]
public sealed class ActorStateConfig
{
    public ActorStateType StateType;
    [SerializeField,HideInInspector]private string stateClassName;

    public string StateClassName=>stateClassName;
}

[Serializable]
public sealed class FirstPersonStateConfig
{
    public FirstPersonStateType StateType;
    [SerializeField,HideInInspector]private string stateClassName;

    public string StateClassName=>stateClassName;
}

[Serializable]
public sealed class FirstPersonGlobalTransitionConfig
{
    [Tooltip("满足进入条件后切换到的目标状态")]
    public FirstPersonStateType TargetState;

    [Tooltip("优先级数值越大越先判断")]
    public int Priority;

    [Tooltip("允许通过这条关系进入目标状态的来源状态")]
    public List<FirstPersonStateType>AllowedFromStates=new();
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
    [InspectorName("Full Body/Idle")]
    Idle=0,
    [InspectorName("Full Body/Move Loop")]
    MoveLoop=2,
    [InspectorName("Full Body/Jump")]
    Jump=4,
    [InspectorName("Full Body/Fall")]
    Fall=5,
    [InspectorName("Full Body/Land")]
    Land=6,
    [InspectorName("Full Body/Aim Idle")]
    AimIdle=7,
    [InspectorName("Full Body/Aim Move")]
    AimMove=8,
    [InspectorName("Shared/Death")]
    Death=9,
}

public enum FirstPersonStateType
{
    Idle=10,
    Move=11,
    Sprint=12,
    Crouch=13,
    Jump=14,
    Fall=15,
    Land=16,
    AimIdle=17,
    AimMove=18,
    TurnLeft=19,
    TurnRight=20,
    GetWeapon=21,
}
