using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActorBrainSo", menuName = "Actor/Brain")]
public class ActorBrainSo : ScriptableObject
{
    [Header("Initial")]
    public CameraPerspectiveMode InitialPerspectiveMode=
        CameraPerspectiveMode.ThirdPerson;

    [Header("Shared")]
    public List<ActorStateConfig>SharedStates=new();
    [Tooltip("可以应用于两种视角状态的转换")]
    public List<ActorGlobalTransitionConfig>SharedTransitions=new();

    [Header("Third Person")]
    public ActorStateGraphConfig ThirdPerson=new()
    {
        InitialState=ActorStateType.Idle,
    };

    [Header("First Person")]
    public ActorStateGraphConfig FirstPerson=new()
    {
        InitialState=ActorStateType.FirstPersonIdle,
    };

    [Header("Perspective Transitions")]
    [Tooltip("第一人称和第三人称状态组之间的转换")]
    public List<ActorGlobalTransitionConfig>PerspectiveTransitions=new();

    [Header("Upper Body")]
    public UpperBodyStateType InitialUpperBodyState=UpperBodyStateType.Empty;
    public List<UpperBodyStateConfig>AvailableUpperBodyStates=new()
    {
        new UpperBodyStateConfig
        {
            StateType=UpperBodyStateType.Empty,
        },
        new UpperBodyStateConfig
        {
            StateType=UpperBodyStateType.Wait,
        },
        new UpperBodyStateConfig
        {
            StateType=UpperBodyStateType.Fire,
        },
    };

    public ActorStateGraphConfig GetGraph(CameraPerspectiveMode perspective)
    {
        return perspective==CameraPerspectiveMode.FirstPerson
            ?FirstPerson
            :ThirdPerson;
    }

    public ActorStateType GetInitialStateType()
    {
        ActorStateGraphConfig graph=GetGraph(InitialPerspectiveMode);
        return graph!=null?graph.InitialState:ActorStateType.Idle;
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
public sealed class ActorStateConfig
{
    public ActorStateType StateType;
    [SerializeField,HideInInspector]private string stateClassName;

    public string StateClassName=>stateClassName;
}

[Serializable]
public sealed class UpperBodyStateConfig
{
    public UpperBodyStateType StateType;
    [SerializeField,HideInInspector]private string stateClassName;

    public string StateClassName=>stateClassName;
}

public enum UpperBodyStateType
{
    Empty,
    Fire,
    Wait,
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
    [InspectorName("Third Person/Idle")]
    Idle,
    [InspectorName("Third Person/Move Start")]
    MoveStart,
    [InspectorName("Third Person/Move Loop")]
    MoveLoop,
    [InspectorName("Third Person/Move Stop")]
    MoveStop,
    [InspectorName("Third Person/Jump")]
    Jump,
    [InspectorName("Third Person/Fall")]
    Fall,
    [InspectorName("Third Person/Land")]
    Land,
    [InspectorName("Third Person/Aim Idle")]
    AimIdle,
    [InspectorName("Third Person/Aim Move")]
    AimMove,
    [InspectorName("Shared/Death")]
    Death,
    [InspectorName("First Person/Idle")]
    FirstPersonIdle,
    [InspectorName("First Person/Move")]
    FirstPersonMove,
    [InspectorName("First Person/Sprint")]
    FirstPersonSprint,
    [InspectorName("First Person/Crouch")]
    FirstPersonCrouch,
    [InspectorName("First Person/Jump")]
    FirstPersonJump,
    [InspectorName("First Person/Fall")]
    FirstPersonFall,
    [InspectorName("First Person/Land")]
    FirstPersonLand,
    [InspectorName("First Person/Aim Idle")]
    FirstPersonAimIdle,
    [InspectorName("First Person/Aim Move")]
    FirstPersonAimMove,
    [InspectorName("First Person/Turn Left")]
    FirstPersonTurnLeft,
    [InspectorName("First Person/Turn Right")]
    FirstPersonTurnRight,
}
