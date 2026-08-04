using System;
using UnityEngine;
using UnityEngine.InputSystem;
[Serializable]
public class RunTimeData
{
    [Header("Input")]
    public ActorInputCommand Input;
    [Header("二级处理")]
    public bool WantMove=>Input.InputMove.magnitude>0.01f;
    [Header("state黑板")]
    public bool StartFootIsL;
}
