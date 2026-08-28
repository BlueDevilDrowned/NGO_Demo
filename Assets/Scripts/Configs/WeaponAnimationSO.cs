using System;
using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnityEngine.Serialization;
public enum E_Weapon
{
    Knife,
    AK12,
}
[CreateAssetMenu(fileName = "WeaponAnimationSO", menuName = "Scriptable Objects/WeaponAnimationSO")]
public class WeaponAnimationSO : ScriptableObject
{
    public E_Weapon Weapon;
    public ThirdPersonAnimation thirdPersonAnimation;
    public FirstPersonAnimation firstPersonAnimation;
}
[Serializable]
public struct StandAnimations
{
    [Header("Stand")]
    public TransitionAsset Idle;
    public TransitionAsset WalkStart_8ways;
    public TransitionAsset Walk_8ways;
    public TransitionAsset WalkStop_L_8ways;
    public TransitionAsset WalkStop_R_8ways;
    public TransitionAsset RunStart_8ways;
    public TransitionAsset Run_8ways;
    public TransitionAsset RunStop_L_8ways;
    public TransitionAsset RunStop_R_8ways;
    public JumpAnimations Jump;
}
[Serializable]
public struct JumpAnimations
{
    public TransitionAsset StandJumpStart;
    public TransitionAsset RunJumpStart;

    public TransitionAsset JumpLoop;
    public TransitionAsset JumpLand;
    public TransitionAsset JumpLandRun;
}
[Serializable]
public struct CrouchAnimations
{
    [Header("Crouch")]
    public TransitionAsset Idle;
    public TransitionAsset Walk_8ways;
    public TransitionAsset Run_8ways;
}
[Serializable]
public struct FirstPersonAnimation
{
    public TransitionAsset Idle_Loop;
    public TransitionAsset Idle_Fire1;
    public TransitionAsset Idle_Fire2;
    
    public TransitionAsset Walk_Loop;
    public TransitionAsset Sprint_Loop;
    public TransitionAsset Run_Loop;
    public TransitionAsset JumpStart;
    public TransitionAsset JumpLoop;
    public TransitionAsset JumpEnd;

    public TransitionAsset Injure;
    public TransitionAsset Inspect;
    public TransitionAsset GetWeapon;
    public TransitionAsset PutWeapon;
    
}
[Serializable]
public struct ThirdPersonAnimation
{
    public StandAnimations stand;
    public TransitionAsset IdleToCrouch;
    public TransitionAsset CrouchToIdle;
    public CrouchAnimations crouch;
}