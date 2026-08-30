using System;
using Animancer;
using UnityEngine;

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
    public DirectionalLocomotionAnimations Jog = new();
    public DirectionalLocomotionAnimations Run = new();
    public DirectionalLocomotionAnimations Sprint = new();
}

[Serializable]
public sealed class DirectionalLocomotionAnimations
{
    [Tooltip("Legacy single start transition. Prefer StartByDirection when root motion data is available.")]
    public TransitionAsset Start;

    [Tooltip("Directional locomotion loop, normally a 2D mixer.")]
    public TransitionAsset Loop;

    [Tooltip("Legacy single stop transition ending on the left foot. Prefer StopLeftFootByDirection.")]
    public TransitionAsset StopLeftFoot;

    [Tooltip("Legacy single stop transition ending on the right foot. Prefer StopRightFootByDirection.")]
    public TransitionAsset StopRightFoot;

    [Tooltip("Optional turning or leaning loop used while moving.")]
    public TransitionAsset Lean;

    [Header("Root Motion By Direction")]
    public DirectionalRootMotionAnimations StartByDirection = new();
    public DirectionalRootMotionAnimations StopLeftFootByDirection = new();
    public DirectionalRootMotionAnimations StopRightFootByDirection = new();

    public RootMotionAnimation GetStart(Vector2 direction)
    {
        return StartByDirection != null && StartByDirection.HasAny
            ? StartByDirection.Select(direction)
            : new RootMotionAnimation { Transition = Start };
    }

    public RootMotionAnimation GetStop(bool endingOnRightFoot, Vector2 direction)
    {
        DirectionalRootMotionAnimations directional = endingOnRightFoot
            ? StopRightFootByDirection
            : StopLeftFootByDirection;
        if (directional != null && directional.HasAny)
            return directional.Select(direction);

        return new RootMotionAnimation
        {
            Transition = endingOnRightFoot ? StopRightFoot : StopLeftFoot,
        };
    }

    public bool HasAnyAnimation =>
        Start != null || Loop != null || StopLeftFoot != null || StopRightFoot != null ||
        Lean != null ||
        (StartByDirection != null && StartByDirection.HasAny) ||
        (StopLeftFootByDirection != null && StopLeftFootByDirection.HasAny) ||
        (StopRightFootByDirection != null && StopRightFootByDirection.HasAny);
}

[Serializable]
public struct RootMotionAnimation
{
    public TransitionAsset Transition;
    public RootMotionData RootData;
}

[Serializable]
public sealed class DirectionalRootMotionAnimations
{
    public RootMotionAnimation Forward;
    public RootMotionAnimation Backward;
    public RootMotionAnimation Left;
    public RootMotionAnimation Right;
    public RootMotionAnimation ForwardLeft;
    public RootMotionAnimation ForwardRight;
    public RootMotionAnimation BackwardLeft;
    public RootMotionAnimation BackwardRight;

    public bool HasAny =>
        Forward.Transition != null || Backward.Transition != null ||
        Left.Transition != null || Right.Transition != null ||
        ForwardLeft.Transition != null || ForwardRight.Transition != null ||
        BackwardLeft.Transition != null || BackwardRight.Transition != null;

    public RootMotionAnimation Select(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return Forward;

        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        int sector = Mathf.RoundToInt(angle / 45f);
        sector = (sector % 8 + 8) % 8;
        return sector switch
        {
            0 => Forward,
            1 => ForwardRight,
            2 => Right,
            3 => BackwardRight,
            4 => Backward,
            5 => BackwardLeft,
            6 => Left,
            _ => ForwardLeft,
        };
    }
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
