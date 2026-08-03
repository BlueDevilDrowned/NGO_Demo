using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ActorBrainSo", menuName = "Actor/Brain")]
public class ActorBrainSo : ScriptableObject
{
    [Tooltip("第一个状态时初始状态")]
    public List<ActorStateType>AvailableStates=new();
}
public enum ActorStateType
{
    Idle,
    WalkStart,
    WalkLoop,
    WalkStop,
}