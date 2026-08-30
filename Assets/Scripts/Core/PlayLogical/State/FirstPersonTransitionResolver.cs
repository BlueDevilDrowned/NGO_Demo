using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resolves first-person presentation transitions from the ActorBrainSo graph.
/// Transition conditions stay in the concrete FirstPersonActorState classes.
/// </summary>
public sealed class FirstPersonTransitionResolver
{
    private sealed class Candidate
    {
        public FirstPersonActorState TargetState;
        public int Priority;
        public int ConfigOrder;
    }

    private readonly FirstPersonStateRegistry stateRegistry;
    private readonly Dictionary<FirstPersonStateType,List<Candidate>>
        candidatesBySource=new();

    public FirstPersonTransitionResolver(
        ActorBrainSo brain,
        FirstPersonStateRegistry stateRegistry)
    {
        this.stateRegistry=stateRegistry??
            throw new ArgumentNullException(nameof(stateRegistry));
        BuildTable(brain);
    }

    public BaseState SelectNextState(BaseState currentState)
    {
        if(currentState is not FirstPersonActorState current||
           !stateRegistry.TryGetStateType(current,out FirstPersonStateType sourceType)||
           !candidatesBySource.TryGetValue(
               sourceType,
               out List<Candidate>candidates))
            return null;

        for(int i=0;i<candidates.Count;i++)
        {
            FirstPersonActorState target=candidates[i].TargetState;
            if(ReferenceEquals(target,current))continue;

            if(target.CanEnterFrom(current))
                return target;
        }

        return null;
    }

    private void BuildTable(ActorBrainSo brain)
    {
        if(brain?.FirstPerson?.Transitions==null)return;

        HashSet<(
            FirstPersonStateType Source,
            FirstPersonStateType Target)>registeredEdges=new();
        int configOrder=0;

        foreach(FirstPersonTransitionConfig config in
                brain.FirstPerson.Transitions)
        {
            int currentOrder=configOrder++;
            if(config==null||config.AllowedFromStates==null)continue;

            if(!stateRegistry.TryGetState(
                   config.TargetState,
                   out FirstPersonActorState targetState))
            {
                Debug.LogError(
                    $"第一人称转换目标状态未注册：{config.TargetState}",
                    brain);
                continue;
            }

            for(int sourceIndex=0;
                sourceIndex<config.AllowedFromStates.Count;
                sourceIndex++)
            {
                FirstPersonStateType sourceType=
                    config.AllowedFromStates[sourceIndex];

                if(sourceType==config.TargetState)
                {
                    Debug.LogWarning(
                        $"已忽略第一人称状态自转换：{sourceType}",
                        brain);
                    continue;
                }

                if(!stateRegistry.TryGetState(sourceType,out _))
                {
                    Debug.LogError(
                        $"第一人称转换来源状态未注册：{sourceType}",
                        brain);
                    continue;
                }

                if(!registeredEdges.Add((sourceType,config.TargetState)))
                {
                    Debug.LogWarning(
                        $"已忽略重复第一人称转换："+
                        $"{sourceType} -> {config.TargetState}",
                        brain);
                    continue;
                }

                if(!candidatesBySource.TryGetValue(
                       sourceType,
                       out List<Candidate>candidates))
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
}
