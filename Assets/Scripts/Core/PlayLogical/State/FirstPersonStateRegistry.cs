using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class FirstPersonStateRegistry
{
    private readonly Dictionary<Type,FirstPersonActorState>states=new();
    private readonly Dictionary<FirstPersonStateType,FirstPersonActorState>
        statesById=new();
    private readonly Dictionary<FirstPersonActorState,FirstPersonStateType>
        stateIds=new();

    public FirstPersonActorState InitialState{get;private set;}

    public void Initialize(ActorBrainSo brain,Actor actor)
    {
        if(brain==null)throw new ArgumentNullException(nameof(brain));
        if(actor==null)throw new ArgumentNullException(nameof(actor));

        FirstPersonStateGraphConfig graph=brain.FirstPerson;
        if(graph?.AvailableStates==null)
            throw new InvalidOperationException(
                "First-person state graph is not configured.");

        foreach(FirstPersonStateConfig config in graph.AvailableStates)
        {
            if(config==null)continue;

            FirstPersonActorState state=CreateState(config,actor);
            if(state==null)continue;

            Type type=state.GetType();
            if(!states.TryAdd(type,state))
            {
                Debug.LogError($"Duplicate first-person state: {type.Name}",brain);
                continue;
            }

            if(!statesById.TryAdd(config.StateType,state))
            {
                states.Remove(type);
                Debug.LogError(
                    $"Duplicate first-person state ID: {config.StateType}",
                    brain);
                continue;
            }

            stateIds.Add(state,config.StateType);
            if(config.StateType==graph.InitialState)
                InitialState=state;
        }

        if(InitialState!=null)return;

        Debug.LogError(
            $"Initial first-person state is not registered: " +
            $"{graph.InitialState}",
            brain);
        foreach(FirstPersonActorState state in statesById.Values)
        {
            InitialState=state;
            break;
        }
    }

    public FirstPersonActorState GetState(FirstPersonStateType stateType)
    {
        if(statesById.TryGetValue(stateType,out FirstPersonActorState state))
            return state;

        Debug.LogError($"First-person state is not registered: {stateType}");
        return null;
    }

    public bool TryGetState(
        FirstPersonStateType stateType,
        out FirstPersonActorState state)
    {
        return statesById.TryGetValue(stateType,out state);
    }

    public bool TryGetStateType(
        FirstPersonActorState state,
        out FirstPersonStateType stateType)
    {
        return stateIds.TryGetValue(state,out stateType);
    }

    private static FirstPersonActorState CreateState(
        FirstPersonStateConfig config,
        Actor actor)
    {
        string className=config.StateClassName;
        if(string.IsNullOrWhiteSpace(className))
        {
            Debug.LogError(
                $"First-person state class is not configured: " +
                $"{config.StateType}");
            return null;
        }

        Type type=Type.GetType(className);
        if(type==null||type.IsAbstract||
           !typeof(FirstPersonActorState).IsAssignableFrom(type))
        {
            Debug.LogError(
                $"Invalid first-person state class for {config.StateType}: " +
                className);
            return null;
        }

        try
        {
            return (FirstPersonActorState)Activator.CreateInstance(
                type,
                new object[]{actor});
        }
        catch(Exception exception)
        {
            Debug.LogError(
                $"Failed to create first-person state {config.StateType}: " +
                className);
            Debug.LogException(exception);
            return null;
        }
    }
}
