using System;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "AnimancerData", menuName = "Scriptable Objects/AnimancerData")]
public class AnimancerData : ScriptableObject
{
    public TransitionAsset Idle;
    [Header("Walk_Locomotion")]
    public LocomotionTransition Walk;
    [Header("JogTransition")]
    public LocomotionTransition Jog;
    [Header("Jump")]
    public JumpTransition Jump;
    [Header("Fall")]
    public TransitionAsset Fall;
    [Header("Landing")]
    public LandingTransition Landing;
    [Header("Aiming")]
    public AimingTransition Aiming;
}
[Serializable]
public struct TransitionAndData
{
    public TransitionAsset transition;
    public RootMotionData data;
}
[Serializable]
public struct JumpTransition
{
    public JumpIdleTransition Idle;
    [FormerlySerializedAs("Run")]
    public RunJumpTransition RunJump;
}
[Serializable]
public struct JumpIdleTransition
{
    public TransitionAndData Jump_1h;
    public TransitionAndData Jump_2h;
    public TransitionAndData Jump_3h;
    public TransitionAndData Jump_AirL;
    public TransitionAndData Jump_AirR;
}
[Serializable]
public struct RunJumpTransition
{
    public TransitionAndData Jump_1h;
    public TransitionAndData Jump_2h;
    public TransitionAndData Jump_AirL;
    public TransitionAndData Jump_AirR;
    public TransitionAndData Jump_hyper;
    public TransitionAndData Jump_obstacle;
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
public struct AimingTransition
{
    public TransitionAsset Idle;
    public TransitionAsset Walk;
    public TransitionAsset Jog;
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
