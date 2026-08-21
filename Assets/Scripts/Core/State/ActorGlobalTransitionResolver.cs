using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ActorGlobalTransitionResolver
{
    private sealed class Candidate
    {
        public ActorBaseState TargetState;
        public int Priority;
        public int ConfigOrder;
    }

    private readonly ActorStateRegistry stateRegistry;
    private readonly Dictionary<ActorStateType,List<Candidate>>candidatesBySource=new();

    public ActorGlobalTransitionResolver(ActorBrainSo brain,ActorStateRegistry stateRegistry)
    {
        this.stateRegistry=stateRegistry??throw new ArgumentNullException(nameof(stateRegistry));
        BuildTable(brain);
    }
    //根据配置表，按优先级看当前状态能切换到哪个状态
    public BaseState SelectNextState(BaseState currentState)
    {
        if(currentState is not ActorBaseState actorCurrent||
           !stateRegistry.TryGetStateType(actorCurrent,out ActorStateType sourceType)||
           !candidatesBySource.TryGetValue(sourceType,out List<Candidate>candidates))
            return null;

        for(int i=0;i<candidates.Count;i++)
        {
            ActorBaseState targetState=candidates[i].TargetState;
            if(targetState.CanEnterFrom(actorCurrent))
                return targetState;
        }

        return null;
    }

    private void BuildTable(ActorBrainSo brain)
    {
        if(brain==null)return;

        HashSet<(ActorStateType Source,ActorStateType Target)>registeredEdges=new();
        int configOrder=0;
        AddTransitions(
            brain.SharedTransitions,
            brain,
            registeredEdges,
            ref configOrder);
        AddTransitions(
            brain.ThirdPerson?.GlobalTransitions,
            brain,
            registeredEdges,
            ref configOrder);
        AddTransitions(
            brain.FirstPerson?.GlobalTransitions,
            brain,
            registeredEdges,
            ref configOrder);
        AddTransitions(
            brain.PerspectiveTransitions,
            brain,
            registeredEdges,
            ref configOrder);

        foreach(List<Candidate>candidates in candidatesBySource.Values)
        {
            candidates.Sort((left,right)=>
            {
                int priorityOrder=right.Priority.CompareTo(left.Priority);
                return priorityOrder!=0
                    ?priorityOrder
                    :left.ConfigOrder.CompareTo(right.ConfigOrder);
            });
        }
    }

    private void AddTransitions(
        List<ActorGlobalTransitionConfig>transitions,
        ActorBrainSo brain,
        HashSet<(ActorStateType Source,ActorStateType Target)>registeredEdges,
        ref int configOrder)
    {
        if(transitions==null)return;

        for(int configIndex=0;configIndex<transitions.Count;configIndex++)
        {
            int currentOrder=configOrder++;
            ActorGlobalTransitionConfig config=transitions[configIndex];
            if(config==null)continue;

            if(!stateRegistry.TryGetState(config.TargetState,out ActorBaseState targetState))
            {
                Debug.LogError($"全局转换目标状态未注册：{config.TargetState}",brain);
                continue;
            }

            if(config.AllowedFromStates==null)continue;

            for(int sourceIndex=0;sourceIndex<config.AllowedFromStates.Count;sourceIndex++)
            {
                ActorStateType sourceType=config.AllowedFromStates[sourceIndex];
                if(sourceType==config.TargetState)
                {
                    Debug.LogWarning($"已忽略全局状态自转换：{sourceType}",brain);
                    continue;
                }

                if(!stateRegistry.TryGetState(sourceType,out _))
                {
                    Debug.LogError($"全局转换来源状态未注册：{sourceType}",brain);
                    continue;
                }

                if(!registeredEdges.Add((sourceType,config.TargetState)))
                {
                    Debug.LogWarning($"已忽略重复全局转换：{sourceType} -> {config.TargetState}",brain);
                    continue;
                }

                if(!candidatesBySource.TryGetValue(sourceType,out List<Candidate>candidates))
                {
                    candidates=new List<Candidate>();
                    candidatesBySource.Add(sourceType,candidates);
                }

                candidates.Add(new Candidate
                {
                    TargetState=targetState,
                    Priority=config.Priority,
                    ConfigOrder=currentOrder,
                });
            }
        }
    }
}
