using System;
using UnityEngine;
using UnityEngine.InputSystem;
[Serializable]
public class RunTimeData
{
    [Header("Input")]
    public ActorInputCommand Input;
    [Header("状态机所需参数（除input之外）")]
    public ActorStateBlackboard blackboard;
    public bool WantMove=>Input.InputMove.magnitude>0.01f;
    
}
