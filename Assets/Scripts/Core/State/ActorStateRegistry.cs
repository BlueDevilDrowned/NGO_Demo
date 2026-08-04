using System;
using System.Collections.Generic;
using UnityEngine;

public class ActorStateRegistry
{
    private static readonly Dictionary<ActorStateType,Func<Actor,ActorBaseState>>StateFactories=new()
    {
        [ActorStateType.Idle]=actor=>new ActorIdleState(actor),
        [ActorStateType.WalkStart]=actor=>new ActorWalkStartState(actor),
        [ActorStateType.WalkLoop]=actor=>new ActorWalkLoopState(actor),
        [ActorStateType.WalkStop]=actor=>new ActorWalkStopState(actor),
    };

    private readonly Dictionary<Type,ActorBaseState>_states=new();
    private readonly Dictionary<ActorStateType,ActorBaseState>statesById=new();
    private readonly Dictionary<ActorBaseState,ActorStateType>stateIds=new();
    public ActorBaseState InitialState{get;private set;}
    public void Initialize(ActorBrainSo brain,Actor actor)
    {
        foreach(ActorStateType stateType in brain.AvailableStates)
        {
            ActorBaseState state=CreateState(stateType,actor);

            if(state==null)continue;

            Type type=state.GetType();
            if(!_states.TryAdd(type,state))
            {
                Debug.LogError($"重复注册状态:{type.Name}");
                continue;
            }
            if(!statesById.TryAdd(stateType, state))
            {
                Debug.LogError($"重复注册状态:{type.Name}");
                continue;
            }

            stateIds.Add(state,stateType);
            InitialState??=state;
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
    public ActorStateType GetStateType(ActorBaseState state)
    {
        if(stateIds.TryGetValue(state,out var stateType))
        {
            return stateType;
        }
        Debug.LogError($"State is not registered: {state?.GetType().Name}");
        return default;
    }
    private static ActorBaseState CreateState(ActorStateType type,Actor actor)
    {
        if(StateFactories.TryGetValue(type,out var factory))
        {
            return factory(actor);
        }
        Debug.LogError($"State factory is not registered: {type}");
        return null;
    }
}
