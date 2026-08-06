using System;
using Animancer;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimancerData", menuName = "Scriptable Objects/AnimancerData")]
public class AnimancerData : ScriptableObject
{
    public TransitionAsset Idle;
    [Header("Walk_Locomotion")]
    public LocomotionTransition Walk;
    [Header("JogTransition")]
    public LocomotionTransition Jog; 
    [Header("WalkTurn")]
    public TransitionAsset Walk_Turn_LL;
    public TransitionAsset Walk_Turn_LR;
    public TransitionAsset Walk_Turn_RL;
    public TransitionAsset Walk_Turn_RR;
}
[Serializable]
public struct TransitionAndData
{
    public TransitionAsset transition;
    public RootMotionData data;
}
[Serializable]
public struct LocomotionTransition
{
    [Header("Start")]
    public TransitionAndData Start_L0;
    public TransitionAndData Start_L45;
    public TransitionAndData Start_L90;
    public TransitionAndData Start_L135;
    public TransitionAndData Start_L180;
    public TransitionAndData Start_R0;
    public TransitionAndData Start_R45;
    public TransitionAndData Start_R90;
    public TransitionAndData Start_R135;
    public TransitionAndData Start_R180;

    [Header("Start")]
    public TransitionAndData Stop_L;
    public TransitionAndData Stop_R;
    [Header("Loop")]
    public TransitionAsset Loop_L;
    public TransitionAsset Loop_R;
    //这个动画数据只代表移动向左向右倾斜，都是右脚起步
    public TransitionAsset Loop_Lean;
}
