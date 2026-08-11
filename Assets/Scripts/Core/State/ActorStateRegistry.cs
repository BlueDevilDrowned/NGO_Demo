using System;
using System.Collections.Generic;
using UnityEngine;

public class ActorStateRegistry
{
    private static readonly Dictionary<ActorStateType,Func<Actor,ActorBaseState>>StateFactories=new()
    {
        [ActorStateType.Idle]=actor=>new ActorIdleState(actor),
        [ActorStateType.MoveStart]=actor=>new ActorMoveStartState(actor),
        [ActorStateType.MoveLoop]=actor=>new ActorMoveLoopState(actor),
        [ActorStateType.MoveStop]=actor=>new ActorMoveStopState(actor),
        [ActorStateType.Jump]=actor=>new ActorJumpState(actor),
        [ActorStateType.Fall]=actor=>new ActorFallState(actor),
        [ActorStateType.Land]=actor=>new ActorLandState(actor),
        [ActorStateType.AimIdle]=actor=>new ActorAimIdleState(actor),
        [ActorStateType.AimMove]=actor=>new ActorAimMoveState(actor),
        [ActorStateType.Death]=actor=>new ActorDeathState(actor),
    };

    private readonly Dictionary<Type,ActorBaseState>_states=new();
    private readonly Dictionary<ActorStateType,ActorBaseState>statesById=new();
    private readonly Dictionary<ActorBaseState,ActorStateType>stateIds=new();
    public ActorBaseState InitialState{get;private set;}
    public void Initialize(ActorBrainSo brain,Actor actor)
    {
        if(brain==null)throw new ArgumentNullException(nameof(brain));
        if(actor==null)throw new ArgumentNullException(nameof(actor));

        foreach(ActorStateConfig config in brain.AvailableStates)
        {
            if(config==null)continue;

            ActorStateType stateType=config.StateType;
            ActorBaseState state=CreateState(stateType,actor);

            if(state==null)continue;
            state.BindConfig(config);

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
            if(stateType==brain.InitialState)
                InitialState=state;
        }

        if(InitialState==null)
        {
            Debug.LogError($"Initial state is not registered: {brain.InitialState}",brain);
            foreach(ActorBaseState state in statesById.Values)
            {
                InitialState=state;
                break;
            }
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
