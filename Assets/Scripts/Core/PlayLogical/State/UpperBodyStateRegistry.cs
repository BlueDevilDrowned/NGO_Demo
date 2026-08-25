using System;
using System.Collections.Generic;
using UnityEngine;

public class UpperBodyStateRegistry
{
    private readonly Dictionary<Type,UpperBodyState> _states=new();
    private readonly Dictionary<UpperBodyStateType,UpperBodyState> _statesById=new();
    private readonly Dictionary<UpperBodyState,UpperBodyStateType> _stateIds=new();

    public UpperBodyState InitialState{get;private set;}

    public void Initialize(ActorBrainSo brain,Actor actor)
    {
        if(brain==null)throw new ArgumentNullException(nameof(brain));
        if(actor==null)throw new ArgumentNullException(nameof(actor));

        foreach(UpperBodyStateConfig config in brain.AvailableUpperBodyStates)
        {
            if(config==null)continue;

            UpperBodyStateType stateType=config.StateType;
            UpperBodyState state=CreateState(config,actor);
            if(state==null)continue;

            Type type=state.GetType();
            if(!_states.TryAdd(type,state))
            {
                Debug.LogError($"Duplicate upper-body state type: {type.Name}");
                continue;
            }

            if(!_statesById.TryAdd(stateType,state))
            {
                _states.Remove(type);
                Debug.LogError($"Duplicate upper-body state ID: {stateType}");
                continue;
            }

            _stateIds.Add(state,stateType);
            if(stateType==brain.InitialUpperBodyState)
                InitialState=state;
        }

        if(InitialState!=null)return;

        Debug.LogError(
            $"Initial upper-body state is not registered: " +
            $"{brain.InitialUpperBodyState}",
            brain);
        foreach(UpperBodyState state in _statesById.Values)
        {
            InitialState=state;
            break;
        }
    }

    public T GetState<T>()where T : UpperBodyState
    {
        if(_states.TryGetValue(typeof(T),out UpperBodyState state))
            return (T)state;

        Debug.LogError($"Upper-body state is not registered: {typeof(T).Name}");
        return null;
    }

    public UpperBodyState GetState(UpperBodyStateType stateType)
    {
        if(_statesById.TryGetValue(stateType,out UpperBodyState state))
            return state;

        Debug.LogError($"Upper-body state is not registered: {stateType}");
        return null;
    }

    public bool TryGetState(
        UpperBodyStateType stateType,
        out UpperBodyState state)
    {
        return _statesById.TryGetValue(stateType,out state);
    }

    public UpperBodyStateType GetStateType(UpperBodyState state)
    {
        if(_stateIds.TryGetValue(state,out UpperBodyStateType stateType))
            return stateType;

        Debug.LogError($"Upper-body state is not registered: {state?.GetType().Name}");
        return default;
    }

    public bool TryGetStateType(
        UpperBodyState state,
        out UpperBodyStateType stateType)
    {
        return _stateIds.TryGetValue(state,out stateType);
    }

    private static UpperBodyState CreateState(
        UpperBodyStateConfig config,
        Actor actor)
    {
        string className=config.StateClassName;
        if(string.IsNullOrWhiteSpace(className))
        {
            Debug.LogError(
                $"Upper-body state class is not configured: " +
                $"{config.StateType}");
            return null;
        }

        Type type=Type.GetType(className);
        if(type==null||type.IsAbstract||
           !typeof(UpperBodyState).IsAssignableFrom(type))
        {
            Debug.LogError(
                $"Invalid upper-body state class for {config.StateType}: " +
                className);
            return null;
        }

        try
        {
            return (UpperBodyState)Activator.CreateInstance(
                type,
                new object[]{actor});
        }
        catch(Exception exception)
        {
            Debug.LogError(
                $"Failed to create upper-body state {config.StateType}: " +
                className);
            Debug.LogException(exception);
            return null;
        }
    }
}
