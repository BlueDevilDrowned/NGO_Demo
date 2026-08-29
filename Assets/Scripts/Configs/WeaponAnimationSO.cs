using System;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;

public enum E_Weapon
{
    Knife,
    AK12,
}

[CreateAssetMenu(
    fileName = "WeaponAnimationSO",
    menuName = "Scriptable Objects/Animation/Weapon Animation")]
public class WeaponAnimationSO : ScriptableObject
{
    public E_Weapon Weapon;

    [FormerlySerializedAs("firstPersonAnimation")]
    public FirstPersonWeaponAnimations FirstPerson = new();

    [FormerlySerializedAs("thirdPersonAnimation")]
    public ThirdPersonUpperBodyAnimations ThirdPersonUpperBody = new();
}

[Serializable]
public sealed class FirstPersonWeaponAnimations
{
    [Header("Pose")]
    public TransitionAsset Idle;
    public TransitionAsset IdleAction1;
    public TransitionAsset IdleAction2;
    public TransitionAsset InjuredIdle;

    [Header("Locomotion")]
    public FirstPersonWeaponLocomotionAnimations Locomotion = new();

    [Header("Airborne")]
    public FirstPersonWeaponAirborneAnimations Airborne = new();

    [Header("Stance")]
    public FirstPersonWeaponStanceAnimations Stance = new();

    [Header("Combat")]
    public FirstPersonWeaponCombatAnimations Combat = new();

    [Header("Equipment")]
    public FirstPersonWeaponEquipmentAnimations Equipment = new();
}

[Serializable]
public sealed class FirstPersonWeaponLocomotionAnimations
{
    public TransitionAsset WalkLoop;
    public TransitionAsset RunLoop;
    public TransitionAsset SprintLoop;
    public TransitionAsset SuperSprintLoop;

    [Header("Transitions")]
    public TransitionAsset WalkToRun;
    public TransitionAsset RunToWalk;
    public TransitionAsset RunToSprint;
    public TransitionAsset SprintToWalk;
    public TransitionAsset RunToSuperSprint;
    public TransitionAsset SuperSprintToWalk;
    public TransitionAsset SprintToIdle;

    [Header("Offset Poses")]
    public TransitionAsset SprintOffsetPose;
    public TransitionAsset SuperSprintOffsetPose;
}

[Serializable]
public sealed class FirstPersonWeaponAirborneAnimations
{
    public TransitionAsset JumpStart;
    public TransitionAsset JumpLoop;
    public TransitionAsset JumpLand;

    [Header("Aiming")]
    public TransitionAsset AimJumpStart;
    public TransitionAsset AimJumpLoop;
    public TransitionAsset AimJumpLand;
}

[Serializable]
public sealed class FirstPersonWeaponStanceAnimations
{
    public TransitionAsset IdleToCrouch;
    public TransitionAsset CrouchToIdle;
    public TransitionAsset IdleToProne;
    public TransitionAsset ProneToIdle;
    public TransitionAsset CrouchToProne;
    public TransitionAsset ProneToCrouch;

    [Header("Prone Locomotion")]
    public TransitionAsset ProneForward;
    public TransitionAsset ProneBackward;
    public TransitionAsset ProneLeft;
    public TransitionAsset ProneRight;
}

[Serializable]
public sealed class FirstPersonWeaponCombatAnimations
{
    [Header("Hip Attack")]
    public TransitionAsset Attack;
    public TransitionAsset AttackLoop;
    public TransitionAsset AttackEnd;

    [Header("Aimed Attack")]
    public TransitionAsset AimAttack;
    public TransitionAsset AimAttackLoop;
    public TransitionAsset AimAttackEnd;

    [Header("Aim")]
    public TransitionAsset AimIdle;
    public TransitionAsset AimIdleAdditive;
    public TransitionAsset AimOn;
    public TransitionAsset AimOff;

    [Header("Reload")]
    public TransitionAsset Reload;
    public TransitionAsset ReloadEmpty;
    public TransitionAsset AimReload;
    public TransitionAsset AimReloadEmpty;

    [Header("Fire Mode")]
    public TransitionAsset ToSingleFire;
    public TransitionAsset ToAutomaticFire;
    public TransitionAsset AimToSingleFire;
    public TransitionAsset AimToAutomaticFire;
}

[Serializable]
public sealed class FirstPersonWeaponEquipmentAnimations
{
    public TransitionAsset EquipInitial;
    public TransitionAsset Equip;
    public TransitionAsset EquipFast;
    public TransitionAsset Unequip;
    public TransitionAsset UnequipFast;
    public TransitionAsset Inspect;
    public TransitionAsset InspectEmpty;
}

[Serializable]
public sealed class ThirdPersonUpperBodyAnimations
{
    [Header("Pose")]
    public TransitionAsset Idle;
    public TransitionAsset IdleAdditive;
    public TransitionAsset InjuredIdle;

    [Header("Locomotion Overlays")]
    public ThirdPersonUpperBodyLocomotionAnimations Locomotion = new();

    [Header("Combat")]
    public ThirdPersonWeaponCombatAnimations Combat = new();

    [Header("Equipment")]
    public ThirdPersonWeaponEquipmentAnimations Equipment = new();

    [Header("Stance")]
    public ThirdPersonUpperBodyStanceAnimations Stance = new();
}

[Serializable]
public sealed class ThirdPersonUpperBodyLocomotionAnimations
{
    public TransitionAsset Walk;
    public TransitionAsset Jog;
    public TransitionAsset Run;
    public TransitionAsset Sprint;
    public TransitionAsset CrouchWalk;
    public TransitionAsset CrouchRun;
    public TransitionAsset ProneMove;
    public TransitionAsset LeanLeft;
    public TransitionAsset LeanRight;
}

[Serializable]
public sealed class ThirdPersonWeaponCombatAnimations
{
    [Header("Attack")]
    public TransitionAsset Attack;
    public TransitionAsset AttackLoop;
    public TransitionAsset AttackEnd;
    public TransitionAsset AlternateAttack;

    [Header("Reload")]
    public TransitionAsset Reload;
    public TransitionAsset ReloadEmpty;

    [Header("Aiming")]
    public TransitionAsset AimIdle;
    public TransitionAsset AimAttack;
    public TransitionAsset AimAttackLoop;
    public TransitionAsset AimAttackEnd;
}

[Serializable]
public sealed class ThirdPersonWeaponEquipmentAnimations
{
    public TransitionAsset Equip;
    public TransitionAsset EquipFast;
    public TransitionAsset Unequip;
    public TransitionAsset UnequipFast;
    public TransitionAsset Inspect;
}

[Serializable]
public sealed class ThirdPersonUpperBodyStanceAnimations
{
    public TransitionAsset CrouchIdle;
    public TransitionAsset ProneIdle;
    public TransitionAsset ProneAimIdle;
    public TransitionAsset CrouchAttack;
    public TransitionAsset ProneAttack;
    public TransitionAsset ProneEquip;
    public TransitionAsset ProneUnequip;
}
