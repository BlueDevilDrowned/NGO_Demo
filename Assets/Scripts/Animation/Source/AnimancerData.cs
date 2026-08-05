using System;
using Animancer;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimancerData", menuName = "Scriptable Objects/AnimancerData")]
public class AnimancerData : ScriptableObject
{
    public TransitionAsset Idle;
    [Header("Walk_Start")]
    public TransitionAndData Walk_Start_L0;
    public TransitionAndData Walk_Start_L45;
    public TransitionAndData Walk_Start_L90;
    public TransitionAndData Walk_Start_L135;
    public TransitionAndData Walk_Start_L180;
    public TransitionAndData Walk_Start_R0;
    public TransitionAndData Walk_Start_R45;
    public TransitionAndData Walk_Start_R90;
    public TransitionAndData Walk_Start_R135;
    public TransitionAndData Walk_Start_R180;

    [Header("Walk_Start")]
    public TransitionAndData Walk_Stop_L;
    public TransitionAndData Walk_Stop_R;
    [Header("Walk_Loop")]
    public TransitionAsset Walk_Loop_L;
    public TransitionAsset Walk_Loop_R;
    //这个动画数据只代表移动向左向右倾斜，都是右脚起步
    public TransitionAsset Walk_Loop_Lean;
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
