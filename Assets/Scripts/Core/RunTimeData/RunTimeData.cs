using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
[Serializable]
public class RunTimeData
{
    [Header("Input")]
    public ActorInputData Input;
    [Header("Movement Intent")]
    public LocomotionData locomotion;
    [Header("Aim")]
    public AimData aim;
    [Header("状态机所需参数（除input之外）")]
    public ActorStateBlackboard blackboard;
    public bool WantMove=>
        locomotion.DesiredWorldMoveDirection.sqrMagnitude>0.0001f;
    public bool WantAim=>Input.IsHeld(InputButtons.InputAim);
}
