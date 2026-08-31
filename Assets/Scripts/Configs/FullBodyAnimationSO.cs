using System;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(
    fileName = "FullBodyAnimationSO",
    menuName = "Scriptable Objects/Animation/Full Body Animation")]
public class FullBodyAnimationSO : ScriptableObject
{
    [Header("Layers")]
    [SerializeField]private AvatarMask upperBodyMask;
    [SerializeField]private AvatarMask hitReactionMask;

    public AvatarMask UpperBodyMask=>upperBodyMask;
    public AvatarMask HitReactionMask=>hitReactionMask;

    [Header("Animations")]
    public StandingFullBodyAnimations Standing = new();
    public CrouchingFullBodyAnimations Crouching = new();
    public ProneFullBodyAnimations Prone = new();
    public AirborneFullBodyAnimations Airborne = new();
    public InjuredFullBodyAnimations Injured = new();
    public FullBodyHitReactionAnimations HitReactions = new();
}

[Serializable]
public sealed class StandingFullBodyAnimations
{
    public TransitionAsset Idle;
    public FullBodyTurnAnimations TurnInPlace = new();
    public DirectionalLocomotionAnimations Walk = new();
    [FormerlySerializedAs("Run")]
    public DirectionalLocomotionAnimations RunSprint = new();
}

[Serializable]
public sealed class DirectionalLocomotionAnimations
{
    [Tooltip("Directional locomotion loop, normally a 2D mixer.")]
    public TransitionAsset Loop;

    [Tooltip("Optional turning or leaning loop used while moving.")]
    public TransitionAsset Lean;

    public bool HasAnyAnimation=>Loop!=null||Lean!=null;
}

[Serializable]
public sealed class FullBodyTurnAnimations
{
    public TransitionAsset Left45;
    public TransitionAsset Left90;
    public TransitionAsset Left135;
    public TransitionAsset Left180;
    public TransitionAsset Right45;
    public TransitionAsset Right90;
    public TransitionAsset Right135;
    public TransitionAsset Right180;
}

[Serializable]
public sealed class CrouchingFullBodyAnimations
{
    public TransitionAsset Enter;
    public TransitionAsset Exit;
    public TransitionAsset Idle;
    public TransitionAsset Walk;
    public TransitionAsset Run;
    public TransitionAsset TurnLeft90;
    public TransitionAsset TurnRight90;
    public TransitionAsset ToProne;
    public TransitionAsset FromProne;
}

[Serializable]
public sealed class ProneFullBodyAnimations
{
    public TransitionAsset EnterFromStanding;
    public TransitionAsset EnterFromCrouching;
    public TransitionAsset ExitToStanding;
    public TransitionAsset ExitToCrouching;
    public TransitionAsset Idle;
    public TransitionAsset Move;
    public TransitionAsset TurnLeft90;
    public TransitionAsset TurnRight90;

    [Header("Supine")]
    public TransitionAsset ToSupine;
    public TransitionAsset SupineIdle;
    public TransitionAsset SupineMove;
    public TransitionAsset SupineTurnLeft90;
    public TransitionAsset SupineTurnRight90;
    public TransitionAsset SupineToStanding;
    public TransitionAsset SupineToCrouching;
    public TransitionAsset SupineToProne;
}

[Serializable]
public sealed class AirborneFullBodyAnimations
{
    [Header("Jump")]
    public TransitionAsset StandingJumpStart;
    public TransitionAsset MovingJumpStart;
    [FormerlySerializedAs("JumpLoop")]
    public TransitionAsset StandingJumpLoop;
    public TransitionAsset MovingJumpLoop;
    public TransitionAsset FallLoop;

    [Header("Landing")]
    public TransitionAsset Land;
    public TransitionAsset LandToMove;

    [Header("Special Jump")]
    public TransitionAsset HyperJump;
    public TransitionAsset ObstacleJump;
}

[Serializable]
public sealed class InjuredFullBodyAnimations
{
    public TransitionAsset Idle;
    public TransitionAsset Walk;
    public TransitionAsset Run;
}

[Serializable]
public sealed class FullBodyHitReactionAnimations
{
    public DirectionalHitAnimations Standing = new();
    public DirectionalHitAnimations Crouching = new();
    public DirectionalHitAnimations Prone = new();
}

[Serializable]
public sealed class DirectionalHitAnimations
{
    public TransitionAsset Front;
    public TransitionAsset Back;
    public TransitionAsset Left;
    public TransitionAsset Right;
}
