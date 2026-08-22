using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

[AttributeUsage(AttributeTargets.Field)]
public sealed class AnimationLayerAttribute : Attribute
{
    public int Layer{get;}

    public AnimationLayerAttribute(int layer)
    {
        Layer=layer;
    }
}

[AttributeUsage(AttributeTargets.Field)]
public sealed class IgnoreAnimationPrewarmAttribute : Attribute
{
}

[Serializable]
public struct AnimationPrewarmEntry
{
    [SerializeField] private TransitionAsset transition;
    [SerializeField,Min(0)] private int layer;

    public TransitionAsset Transition=>transition;
    public int Layer=>layer;

    public AnimationPrewarmEntry(TransitionAsset transition,int layer)
    {
        this.transition=transition;
        this.layer=layer;
    }
}

[CreateAssetMenu(fileName = "AnimancerData", menuName = "Scriptable Objects/AnimancerData")]
public class AnimancerData : ScriptableObject
{
    [Header("Layers")]
    [SerializeField] private AvatarMask upperBodyMask;
    [SerializeField] private AvatarMask hitReactionMask;

    public AvatarMask UpperBodyMask=>upperBodyMask;
    public AvatarMask HitReactionMask=>hitReactionMask;
    [Header("Shared")]
    [AnimationLayer(1)]
    public TransitionAsset Fire;

    [Header("Third Person")]
    public ThirdPersonAnimationTransitions ThirdPerson=new();

    [Header("First Person")]
    public FirstPersonAnimationTransitions FirstPerson=new();

    [Header("Hit Reaction")]
    [AnimationLayer(2)]
    public HitReactionTransitions HitReaction;

    [SerializeField,HideInInspector,IgnoreAnimationPrewarm]
    private List<AnimationPrewarmEntry> prewarmEntries=new();

    public IReadOnlyList<AnimationPrewarmEntry> PrewarmEntries=>prewarmEntries;

#if UNITY_EDITOR
    public void ReplacePrewarmEntries(List<AnimationPrewarmEntry> entries)
    {
        prewarmEntries=entries??new List<AnimationPrewarmEntry>();
    }
#endif
}

[Serializable]
public sealed class ThirdPersonAnimationTransitions
{
    [Header("Idle")]
    public TransitionAsset Idle;

    [Header("Walk Locomotion")]
    public LocomotionTransition Walk;

    [Header("Jog Locomotion")]
    public LocomotionTransition Jog;

    [Header("Airborne")]
    public JumpTransition Jump;
    public TransitionAsset Fall;
    public LandingTransition Landing;

    [Header("Aiming")]
    public AimingTransition Aiming;
}

[Serializable]
public sealed class FirstPersonAnimationTransitions
{
    [Header("Idle")]
    public TransitionAsset Idle;
    public TransitionAsset AimIdle;
    public TransitionAsset CrouchIdle;
    public TransitionAsset CrouchAimIdle;
    public TransitionAndData TurnLeft;
    public TransitionAndData TurnRight;

    [Header("Locomotion 2D")]
    public TransitionAsset Walk;
    public TransitionAsset Run;
    public TransitionAsset Sprint;

    [Header("Airborne")]
    public TransitionAsset JumpUp;
    public TransitionAsset JumpLoop;
    public TransitionAsset JumpDown;
}

public enum HitReactionDirection : byte
{
    Front,
    Back,
    Left,
    Right,
}

[Serializable]
public struct DirectionalHitReactionTransitions
{
    public TransitionAsset Front;
    public TransitionAsset Back;
    public TransitionAsset Left;
    public TransitionAsset Right;
    public TransitionAsset Fallback;

    public TransitionAsset Get(HitReactionDirection direction)
    {
        TransitionAsset transition=direction switch
        {
            HitReactionDirection.Front=>Front,
            HitReactionDirection.Back=>Back,
            HitReactionDirection.Left=>Left,
            HitReactionDirection.Right=>Right,
            _=>null,
        };
        return transition!=null?transition:Fallback;
    }
}

[Serializable]
public struct HitReactionTransitions
{
    [Min(0f)]public float FadeInDuration;
    [Min(0f)]public float FadeOutDuration;
    [Tooltip("Ignore hit location and direction, and always play Single Transition.")]
    public bool UseSingleTransition;
    public TransitionAsset SingleTransition;
    public DirectionalHitReactionTransitions Default;
    public DirectionalHitReactionTransitions Head;
    public DirectionalHitReactionTransitions UpperBody;
    public DirectionalHitReactionTransitions LowerBody;

    public TransitionAsset Get(
        HitLocation location,
        HitReactionDirection direction)
    {
        if(UseSingleTransition)return SingleTransition;

        DirectionalHitReactionTransitions group=location switch
        {
            HitLocation.Head or HitLocation.Neck=>Head,
            HitLocation.Chest or HitLocation.Abdomen or
            HitLocation.LeftUpperArm or HitLocation.RightUpperArm or
            HitLocation.LeftForearm or HitLocation.RightForearm or
            HitLocation.LeftHand or HitLocation.RightHand=>UpperBody,
            HitLocation.Pelvis or HitLocation.LeftThigh or
            HitLocation.RightThigh or HitLocation.LeftLowerLeg or
            HitLocation.RightLowerLeg or HitLocation.LeftFoot or
            HitLocation.RightFoot=>LowerBody,
            _=>Default,
        };
        return group.Get(direction)??Default.Get(direction);
    }
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
