using System;
using System.Collections.Generic;
using UnityEngine;

public class ActorStateRegistry
{
    private readonly Dictionary<Type,ActorBaseState>_states=new();
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
    private static ActorBaseState CreateState(ActorStateType type,Actor actor)
    {
        return type switch
        {
            ActorStateType.Idle => throw new NotImplementedException(),
            ActorStateType.WalkStart => throw new NotImplementedException(),
            ActorStateType.WalkLoop => throw new NotImplementedException(),
            ActorStateType.WalkStop => throw new NotImplementedException(),
            _=>null,
        };
    }
}
