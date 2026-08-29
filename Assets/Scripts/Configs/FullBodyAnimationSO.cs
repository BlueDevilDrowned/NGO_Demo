using System;
using Animancer;
using UnityEngine;

[CreateAssetMenu(
    fileName = "FullBodyAnimationSO",
    menuName = "Scriptable Objects/Animation/Full Body Animation")]
public class FullBodyAnimationSO : ScriptableObject
{
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
    public DirectionalLocomotionAnimations Jog = new();
    public DirectionalLocomotionAnimations Run = new();
    public DirectionalLocomotionAnimations Sprint = new();
}

[Serializable]
public sealed class DirectionalLocomotionAnimations
{
    [Tooltip("Directional start animation, normally a 2D mixer.")]
    public TransitionAsset Start;

    [Tooltip("Directional locomotion loop, normally a 2D mixer.")]
    public TransitionAsset Loop;

    [Tooltip("Directional stop animation ending on the left foot.")]
    public TransitionAsset StopLeftFoot;

    [Tooltip("Directional stop animation ending on the right foot.")]
    public TransitionAsset StopRightFoot;

    [Tooltip("Optional turning or leaning loop used while moving.")]
    public TransitionAsset Lean;
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
    public TransitionAsset JumpLoop;
    public TransitionAsset FallLoop;

    [Header("Landing")]
    public TransitionAsset Land;
    public TransitionAsset LandToMove;
    public TransitionAsset HardLand;
    public TransitionAsset StumbleLand;

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
