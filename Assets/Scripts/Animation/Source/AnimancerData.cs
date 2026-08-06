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
    [Header("Landing")]
    public LandingTransition Landing;
}
[Serializable]
public struct TransitionAndData
{
    public TransitionAsset transition;
    public RootMotionData data;
}
[Serializable]
public struct LandingTransition
{
    [Header("Land")]
    public TransitionAndData Land_1h;
    public TransitionAndData Land_2h;
    public TransitionAndData Land_3h;
    public TransitionAndData Land_4h;

    [Header("Land To Run")]
    public TransitionAndData Land_ToRun1;
    public TransitionAndData Land_ToRun2;
    public TransitionAndData Land_ToRun3;
    public TransitionAndData Land_ToSlam;
    public TransitionAndData Land_ToStumble;
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
