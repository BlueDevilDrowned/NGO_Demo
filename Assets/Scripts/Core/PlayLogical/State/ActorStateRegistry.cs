using System;
using System.Collections.Generic;
using UnityEngine;

public class ActorStateRegistry
{
    private readonly Dictionary<Type,ActorBaseState>_states=new();
    private readonly Dictionary<ActorStateType,ActorBaseState>statesById=new();
    private readonly Dictionary<ActorBaseState,ActorStateType>stateIds=new();
    public ActorBaseState InitialState{get;private set;}
    public void Initialize(ActorBrainSo brain,Actor actor)
    {
        if(brain==null)throw new ArgumentNullException(nameof(brain));
        if(actor==null)throw new ArgumentNullException(nameof(actor));

        RegisterStates(brain.SharedStates,actor);
        RegisterGraph(brain.FullBody,actor);

        ActorStateType initialStateType=brain.GetInitialStateType();
        if(statesById.TryGetValue(initialStateType,out ActorBaseState initialState))
            InitialState=initialState;

        if(InitialState==null)
        {
            Debug.LogError(
                $"Initial state is not registered: {initialStateType}",
                brain);
            foreach(ActorBaseState state in statesById.Values)
            {
                InitialState=state;
                break;
            }
        }
    }

    private void RegisterGraph(ActorStateGraphConfig graph,Actor actor)
    {
        if(graph!=null)
            RegisterStates(graph.AvailableStates,actor);
    }

    private void RegisterStates(
        List<ActorStateConfig>configs,
        Actor actor)
    {
        if(configs==null)return;

        foreach(ActorStateConfig config in configs)
        {
            if(config==null)continue;

            ActorStateType stateType=config.StateType;
            ActorBaseState state=CreateState(config,actor);

            if(state==null)continue;

            Type type=state.GetType();
            if(!_states.TryAdd(type,state))
            {
                Debug.LogError($"重复注册状态:{type.Name}");
                continue;
            }
            if(!statesById.TryAdd(stateType, state))
            {
                _states.Remove(type);
                Debug.LogError($"重复注册状态:{type.Name}");
                continue;
            }

            stateIds.Add(state,stateType);
        }
    }
    public T GetState<T>()where T : ActorBaseState
    {
        if(_states.TryGetValue(typeof(T),out var state))
        {
            return (T)state;
        }
        Debug.LogError($"状态未注册：{typeof(T).Name}");
        return null;
    }
    public ActorBaseState GetState(ActorStateType stateType)
    {
        if(statesById.TryGetValue(stateType,out var state))
        {
            return state;
        }
        Debug.LogError($"状态未注册：{stateType}");
        return null;
    }
    public bool TryGetState(ActorStateType stateType,out ActorBaseState state)
    {
        return statesById.TryGetValue(stateType,out state);
    }
    public ActorStateType GetStateType(ActorBaseState state)
    {
        if(stateIds.TryGetValue(state,out var stateType))
        {
            return stateType;
        }
        Debug.LogError($"State is not registered: {state?.GetType().Name}");
        return default;
    }
    public bool TryGetStateType(ActorBaseState state,out ActorStateType stateType)
    {
        return stateIds.TryGetValue(state,out stateType);
    }
    private static ActorBaseState CreateState(
        ActorStateConfig config,
        Actor actor)
    {
        string className=config.StateClassName;
        if(string.IsNullOrWhiteSpace(className))
        {
            Debug.LogError($"State class is not configured: {config.StateType}");
            return null;
        }

        Type type=Type.GetType(className);
        if(type==null||type.IsAbstract||
           !typeof(ActorBaseState).IsAssignableFrom(type))
        {
            Debug.LogError(
                $"Invalid state class for {config.StateType}: {className}");
            return null;
        }

        try
        {
            return (ActorBaseState)Activator.CreateInstance(
                type,
                new object[]{actor});
        }
        catch(Exception exception)
        {
            Debug.LogError(
                $"Failed to create state {config.StateType}: {className}");
            Debug.LogException(exception);
            return null;
        }
    }
}
