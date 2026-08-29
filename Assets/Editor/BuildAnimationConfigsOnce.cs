using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Animancer;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class BuildAnimationConfigsOnce
{
    private const string SessionKey = "NGO.BuildAnimationConfigsOnce.v2";
    private const string GeneratedRoot = "Assets/Animation/Generated";
    private const string ConfigRoot = "Assets/Config/AnimationConfig";
    private const string Unarmed3P = "Assets/Art/买小文/动画/Unarmed/3P";
    private const string Knife1P = "Assets/Art/买小文/动画/knife/1p";
    private const string Knife3P = "Assets/Art/买小文/动画/knife/3P";
    private const string Ak121P = "Assets/Art/买小文/动画/AR_AK_12_1P";
    private const string Rifle3P = "Assets/Art/持枪少女动画";

    private static readonly Vector2 F = new(0f, 1f);
    private static readonly Vector2 B = new(0f, -1f);
    private static readonly Vector2 L = new(-1f, 0f);
    private static readonly Vector2 R = new(1f, 0f);
    private static readonly Vector2 FL = new(-0.70710677f, 0.70710677f);
    private static readonly Vector2 FR = new(0.70710677f, 0.70710677f);
    private static readonly Vector2 BL = new(-0.70710677f, -0.70710677f);
    private static readonly Vector2 BR = new(0.70710677f, -0.70710677f);

    private static readonly List<string> Warnings = new();

    static BuildAnimationConfigsOnce()
    {
        if (!SessionState.GetBool(SessionKey, false))
            EditorApplication.delayCall += Build;
    }

    [MenuItem("Tools/NGO/Animation/Build Animation Configs")]
    public static void Build()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Build;
            return;
        }

        Warnings.Clear();

        try
        {
            EnsureFolder(GeneratedRoot);
            ConfigureFullBody();
            ConfigureKnife();
            ConfigureAk12();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SessionState.SetBool(SessionKey, true);

            string warningText = Warnings.Count == 0
                ? "none"
                : string.Join(" | ", Warnings);
            Debug.Log($"ANIMATION_CONFIG_BUILD_COMPLETE warnings={Warnings.Count}: {warningText}");
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private static void ConfigureFullBody()
    {
        FullBodyAnimationSO config = LoadOrCreate<FullBodyAnimationSO>(
            $"{ConfigRoot}/FullBodyAnimationSO.asset");

        config.Standing ??= new();
        config.Standing.TurnInPlace ??= new();
        config.Standing.Walk ??= new();
        config.Standing.Jog ??= new();
        config.Standing.Run ??= new();
        config.Standing.Sprint ??= new();
        config.Crouching ??= new();
        config.Prone ??= new();
        config.Airborne ??= new();
        config.Injured ??= new();
        config.HitReactions ??= new();
        config.HitReactions.Standing ??= new();
        config.HitReactions.Crouching ??= new();
        config.HitReactions.Prone ??= new();

        config.Standing.Idle = Clip("FullBody/Standing", "Idle",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_Idle01.fbx");
        config.Standing.TurnInPlace.Left90 = Clip("FullBody/Standing", "TurnLeft90",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_TurnLeft90.fbx");
        config.Standing.TurnInPlace.Right90 = Clip("FullBody/Standing", "TurnRight90",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_TurnRight90.fbx");

        config.Standing.Walk.Loop = Mixer("FullBody/Standing", "Walk8Way",
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_WalkF.fbx", F),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_WalkB.fbx", B),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_WalkL.fbx", L),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_WalkR.fbx", R),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_WalkFL.fbx", FL),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_WalkFR.fbx", FR),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_WalkBL.fbx", BL),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_WalkBR.fbx", BR));

        config.Standing.Run.Start = Mixer("FullBody/Standing", "RunStart8Way",
            KnifeRunStart("F", F), KnifeRunStart("B", B), KnifeRunStart("L", L), KnifeRunStart("R", R),
            KnifeRunStart("FL", FL), KnifeRunStart("FR", FR), KnifeRunStart("BL", BL), KnifeRunStart("BR", BR));
        config.Standing.Run.Loop = Mixer("FullBody/Standing", "Run8Way",
            KnifeRun("F", F), KnifeRun("B", B), KnifeRun("L", L), KnifeRun("R", R),
            KnifeRun("FL", FL), KnifeRun("FR", FR), KnifeRun("BL", BL), KnifeRun("BR", BR));
        config.Standing.Run.StopLeftFoot = Mixer("FullBody/Standing", "RunStopLeftFoot8Way",
            KnifeRunStop("F", "", F), KnifeRunStop("B", "", B), KnifeRunStop("L", "", L), KnifeRunStop("R", "", R),
            KnifeRunStop("FL", "_L", FL), KnifeRunStop("FR", "_L", FR),
            KnifeRunStop("BL", "_L", BL), KnifeRunStop("BR", "_L", BR));
        config.Standing.Run.StopRightFoot = Mixer("FullBody/Standing", "RunStopRightFoot8Way",
            KnifeRunStop("F", "2", F), KnifeRunStop("B", "", B), KnifeRunStop("L", "2", L), KnifeRunStop("R", "2", R),
            KnifeRunStop("FL", "_R", FL), KnifeRunStop("FR", "_R", FR),
            KnifeRunStop("BL", "_R", BL), KnifeRunStop("BR", "_R", BR));

        config.Standing.Sprint.Loop = Mixer("FullBody/Standing", "SprintForward",
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_SprintF.fbx", F),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_SprintFL.fbx", FL),
            D($"{Unarmed3P}/Normal/Unarmed_M_3P_SprintFr.fbx", FR));
        config.Standing.Sprint.StopLeftFoot = Clip("FullBody/Standing", "SprintToIdle",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_Sprint2Idle.fbx");

        config.Crouching.Enter = Clip("FullBody/Crouching", "Enter",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_Idle2Crouch.fbx");
        config.Crouching.Exit = Clip("FullBody/Crouching", "Exit",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_Crouch2Idle.fbx");
        config.Crouching.Idle = Clip("FullBody/Crouching", "Idle",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_CrouchIdle.fbx");
        config.Crouching.Walk = Mixer("FullBody/Crouching", "Walk8Way",
            UnarmedCrouchWalk("F", F), UnarmedCrouchWalk("B", B),
            UnarmedCrouchWalk("L", L), UnarmedCrouchWalk("R", R),
            UnarmedCrouchWalk("FL", FL), UnarmedCrouchWalk("FR", FR),
            UnarmedCrouchWalk("BL", BL), UnarmedCrouchWalk("BR", BR));
        config.Crouching.Run = Mixer("FullBody/Crouching", "Run8Way",
            KnifeCrouchRun("F", F), KnifeCrouchRun("B", B), KnifeCrouchRun("L", L), KnifeCrouchRun("R", R),
            KnifeCrouchRun("FL", FL), KnifeCrouchRun("FR", FR), KnifeCrouchRun("BL", BL), KnifeCrouchRun("BR", BR));
        config.Crouching.TurnLeft90 = Clip("FullBody/Crouching", "TurnLeft90",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_CrouchTurnLeft90.fbx");
        config.Crouching.TurnRight90 = Clip("FullBody/Crouching", "TurnRight90",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_CrouchTurnRight90.fbx");
        config.Crouching.ToProne = Clip("FullBody/Crouching", "ToProne",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_Crouch2Prone.fbx");
        config.Crouching.FromProne = Clip("FullBody/Crouching", "FromProne",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_Prone2Crouch.fbx");

        config.Prone.EnterFromStanding = Clip("FullBody/Prone", "EnterFromStanding",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_Idle2Prone.fbx");
        config.Prone.EnterFromCrouching = config.Crouching.ToProne;
        config.Prone.ExitToStanding = Clip("FullBody/Prone", "ExitToStanding",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_Prone2Idle.fbx");
        config.Prone.ExitToCrouching = config.Crouching.FromProne;
        config.Prone.Idle = Clip("FullBody/Prone", "Idle",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_ProneIdle.fbx");
        config.Prone.Move = Mixer("FullBody/Prone", "Move8Way",
            UnarmedProne("F", F), UnarmedProne("B", B), UnarmedProne("L", L), UnarmedProne("R", R),
            UnarmedProne("FL", FL), UnarmedProne("FR", FR), UnarmedProne("BL", BL), UnarmedProne("BR", BR));
        config.Prone.TurnLeft90 = Clip("FullBody/Prone", "TurnLeft90",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_ProneTurnLeft90.fbx");
        config.Prone.TurnRight90 = Clip("FullBody/Prone", "TurnRight90",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_ProneTurnRight90.fbx");

        string supine = $"{Knife3P}/supine";
        config.Prone.ToSupine = Clip("FullBody/Supine", "EnterFromStanding",
            $"{supine}/Meless_M_Knife_3P_Stand2Supine.fbx");
        config.Prone.SupineIdle = Clip("FullBody/Supine", "Idle",
            $"{supine}/Meless_M_Knife_3P_SupineIdle.fbx");
        config.Prone.SupineMove = Mixer("FullBody/Supine", "Move8Way",
            KnifeSupineRun("F", F), KnifeSupineRun("B", B), KnifeSupineRun("L", L), KnifeSupineRun("R", R),
            KnifeSupineRun("FL", FL), KnifeSupineRun("FR", FR), KnifeSupineRun("BL", BL), KnifeSupineRun("BR", BR));
        config.Prone.SupineTurnLeft90 = Clip("FullBody/Supine", "TurnLeft90",
            $"{supine}/Meless_M_Knife_3P_Supine_TurnLeft90.fbx");
        config.Prone.SupineTurnRight90 = Clip("FullBody/Supine", "TurnRight90",
            $"{supine}/Meless_M_Knife_3P_Supine_TurnRight90.fbx");
        config.Prone.SupineToStanding = Clip("FullBody/Supine", "ExitToStanding",
            $"{supine}/Meless_M_Knife_3P_Supine2Stand.fbx");
        config.Prone.SupineToCrouching = Clip("FullBody/Supine", "ExitToCrouching",
            $"{supine}/Meless_M_Knife_3P_Supine2Crouch.fbx");
        config.Prone.SupineToProne = Clip("FullBody/Supine", "ExitToProne",
            $"{supine}/Meless_M_Knife_3P_Supine2Prone.fbx");

        config.Airborne.StandingJumpStart = Clip("FullBody/Airborne", "StandingJumpStart",
            $"{Knife3P}/Meless_M_Knife_3P_StandJumpStart.fbx");
        config.Airborne.MovingJumpStart = Clip("FullBody/Airborne", "MovingJumpStart",
            $"{Knife3P}/Meless_M_Knife_3P_RunJumpStart.fbx");
        config.Airborne.JumpLoop = Clip("FullBody/Airborne", "JumpLoop",
            $"{Knife3P}/Meless_M_Knife_3P_StandJumpLoop.fbx");
        config.Airborne.FallLoop = Clip("FullBody/Airborne", "FallLoop",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_FallHLoopH.fbx");
        config.Airborne.Land = Clip("FullBody/Airborne", "Land",
            $"{Knife3P}/Meless_M_Knife_3P_StandJumpLand.fbx");
        config.Airborne.LandToMove = Clip("FullBody/Airborne", "LandToMove",
            $"{Knife3P}/Meless_M_Knife_3P_JumpLandRun.fbx");
        config.Airborne.HardLand = Clip("FullBody/Airborne", "HardLand",
            $"{Unarmed3P}/Normal/Unarmed_M_3P_landH.fbx");

        config.Injured.Idle = Clip("FullBody/Injured", "Idle",
            $"{Unarmed3P}/3p_injure/Unarmed_M_3P_InjuredIdle_knife.fbx");
        config.Injured.Walk = Mixer("FullBody/Injured", "Walk8Way",
            Injured("Walk", "F", F), Injured("Walk", "B", B), Injured("Walk", "L", L), Injured("Walk", "R", R),
            Injured("Walk", "FL", FL), Injured("Walk", "FR", FR), Injured("Walk", "BL", BL), Injured("Walk", "BR", BR));
        config.Injured.Run = Mixer("FullBody/Injured", "Run8Way",
            Injured("Run", "F", F), Injured("Run", "B", B), Injured("Run", "L", L), Injured("Run", "R", R),
            Injured("Run", "FL", FL), Injured("Run", "FR", FR), Injured("Run", "BL", BL), Injured("Run", "BR", BR));

        config.HitReactions.Standing.Front = Clip("FullBody/Hit/Standing", "Front",
            $"{Unarmed3P}/Normal/Earthquake_HitF.fbx");
        config.HitReactions.Standing.Back = Clip("FullBody/Hit/Standing", "Back",
            $"{Unarmed3P}/Normal/Earthquake_HitB.fbx");
        config.HitReactions.Standing.Left = Clip("FullBody/Hit/Standing", "Left",
            $"{Unarmed3P}/Normal/Earthquake_HitL.fbx");
        config.HitReactions.Standing.Right = Clip("FullBody/Hit/Standing", "Right",
            $"{Unarmed3P}/Normal/Earthquake_HitR.fbx");
        config.HitReactions.Crouching.Front = KnifeHit("CrouchSpineF", "Crouching", "Front");
        config.HitReactions.Crouching.Back = KnifeHit("CrouchSpineB", "Crouching", "Back");
        config.HitReactions.Crouching.Left = KnifeHit("CrouchSpineL", "Crouching", "Left");
        config.HitReactions.Crouching.Right = KnifeHit("CrouchSpineR", "Crouching", "Right");
        config.HitReactions.Prone.Front = KnifeHit("ProneHairF", "Prone", "Front");
        config.HitReactions.Prone.Back = KnifeHit("ProneHairB", "Prone", "Back");
        config.HitReactions.Prone.Left = KnifeHit("ProneHairL", "Prone", "Left");
        config.HitReactions.Prone.Right = KnifeHit("ProneHairR", "Prone", "Right");

        EditorUtility.SetDirty(config);
    }

    private static void ConfigureKnife()
    {
        WeaponAnimationSO config = LoadOrCreate<WeaponAnimationSO>(
            $"{ConfigRoot}/WeaponAnimations/Knife.asset");
        config.Weapon = E_Weapon.Knife;
        EnsureWeaponGroups(config);

        FirstPersonWeaponAnimations fp = config.FirstPerson;
        fp.Idle = Knife1("Idle", "Melee_M_Knife_1P_Idle_Loop.fbx");
        fp.IdleAction1 = Knife1("IdleAction1", "Melee_M_Knife_1P_Idle_fire_01.fbx");
        fp.IdleAction2 = Knife1("IdleAction2", "Melee_M_Knife_1P_Idle_fire_02.fbx");
        fp.InjuredIdle = Knife1("InjuredIdle", "Melee_M_Knife_1P_InjuredIdle.fbx");
        fp.Locomotion.WalkLoop = Knife1("WalkLoop", "Melee_M_Knife_1P_Walk_Loop.fbx");
        fp.Locomotion.RunLoop = Knife1("RunLoop", "Melee_M_Knife_1P_Run_Loop.fbx");
        fp.Locomotion.SprintLoop = Knife1("SprintLoop", "Melee_M_Knife_1P_Sprint_Loop.fbx");
        fp.Locomotion.SuperSprintLoop = Knife1("SuperSprintLoop", "Melee_M_Knife_1P_SuperSprint_loop.fbx");
        fp.Locomotion.WalkToRun = Knife1("WalkToRun", "Melee_M_Knife_1P_WalkToRun.fbx");
        fp.Locomotion.RunToWalk = Knife1("RunToWalk", "Melee_M_Knife_1P_RunToWalk.fbx");
        fp.Locomotion.RunToSprint = Knife1("RunToSprint", "Melee_M_Knife_1P_RunToSprint.fbx");
        fp.Locomotion.SprintToWalk = Knife1("SprintToWalk", "Melee_M_Knife_1P_SprintToWalk.fbx");
        fp.Locomotion.RunToSuperSprint = Knife1("RunToSuperSprint", "Melee_M_Knife_1P_RunToSuperSprint.fbx");
        fp.Locomotion.SuperSprintToWalk = Knife1("SuperSprintToWalk", "Melee_M_Knife_1P_SuperSprintToWalk.fbx");
        fp.Locomotion.SprintToIdle = Knife1("SprintToIdle", "Melee_M_Knife_1P_Sprint2idle.fbx");
        fp.Locomotion.SprintOffsetPose = Knife1("SprintOffsetPose", "Melee_M_Knife_1P_sprint_pose.fbx");
        fp.Locomotion.SuperSprintOffsetPose = Knife1("SuperSprintOffsetPose", "Melee_M_Knife_1P_SuperSprint_Pose.fbx");
        fp.Airborne.JumpStart = Knife1("JumpStart", "Melee_M_Knife_1P_JumpStart.fbx");
        fp.Airborne.JumpLoop = Knife1("JumpLoop", "Melee_M_Knife_1P_JumpLoop.fbx");
        fp.Airborne.JumpLand = Knife1("JumpLand", "Melee_M_Knife_1P_JumpLand.fbx");
        fp.Stance.IdleToCrouch = Knife1("IdleToCrouch", "Melee_M_Knife_1P_idle2Crouch.fbx");
        fp.Stance.CrouchToIdle = Knife1("CrouchToIdle", "Melee_M_Knife_1P_Crouch2Idle.fbx");
        fp.Stance.IdleToProne = Knife1("IdleToProne", "Melee_M_Knife_1P_idle2Prone.fbx");
        fp.Stance.ProneToIdle = Knife1("ProneToIdle", "Melee_M_Knife_1P_Prone2Idle.fbx");
        fp.Stance.CrouchToProne = Knife1("CrouchToProne", "Melee_M_Knife_1P_Crouch2Prone.fbx");
        fp.Stance.ProneToCrouch = Knife1("ProneToCrouch", "Melee_M_Knife_1P_Prone2Crouch.fbx");
        fp.Stance.ProneForward = Knife1("ProneForward", "Melee_M_Knife_1P_ProneF.fbx");
        fp.Stance.ProneBackward = Knife1("ProneBackward", "Melee_M_Knife_1P_ProneB.fbx");
        fp.Stance.ProneLeft = Knife1("ProneLeft", "Melee_M_Knife_1P_ProneL.fbx");
        fp.Stance.ProneRight = Knife1("ProneRight", "Melee_M_Knife_1P_ProneR.fbx");
        fp.Combat.Attack = Knife1("Attack", "Melee_M_Knife_1P_Fire+_Montage.fbx");
        fp.Combat.AttackLoop = Knife1("AttackLoop", "Melee_M_Knife_1P_Fire+_Montage_SEQ1.fbx");
        fp.Equipment.Equip = Knife1("Equip", "Melee_M_Knife_1P_Getweapon.fbx");
        fp.Equipment.Unequip = Knife1("Unequip", "Melee_M_Knife_1P_Putweapon.fbx");
        fp.Equipment.Inspect = Knife1("Inspect", "Melee_M_Knife_1P_Inspect.fbx");
        ThirdPersonUpperBodyAnimations tp = config.ThirdPersonUpperBody;
        tp.Idle = Knife3("Idle", "Melee_M_Knife_3P_idle.fbx");
        tp.IdleAdditive = Knife3("IdleAdditive", "Melee_M_Knife_3P_idleadditive.fbx");
        tp.InjuredIdle = Clip("Weapon/Knife/ThirdPerson", "InjuredIdle",
            $"{Unarmed3P}/3p_injure/Unarmed_M_3P_InjuredIdle_knife.fbx");
        tp.Locomotion.Walk = Mixer("Weapon/Knife/ThirdPerson", "Walk8Way",
            KnifeWalk("F", F), KnifeWalk("B", B), KnifeWalk("L", L), KnifeWalk("R", R),
            KnifeWalk("FL", FL), KnifeWalk("FR", FR), KnifeWalk("BL", BL), KnifeWalk("BR", BR));
        tp.Locomotion.Run = Mixer("Weapon/Knife/ThirdPerson", "Run8Way",
            KnifeRun("F", F), KnifeRun("B", B), KnifeRun("L", L), KnifeRun("R", R),
            KnifeRun("FL", FL), KnifeRun("FR", FR), KnifeRun("BL", BL), KnifeRun("BR", BR));
        tp.Locomotion.CrouchWalk = Mixer("Weapon/Knife/ThirdPerson", "CrouchWalk8Way",
            KnifeCrouchWalk("F", F), KnifeCrouchWalk("B", B), KnifeCrouchWalk("L", L), KnifeCrouchWalk("R", R),
            KnifeCrouchWalk("FL", FL), KnifeCrouchWalk("FR", FR), KnifeCrouchWalk("BL", BL), KnifeCrouchWalk("BR", BR));
        tp.Locomotion.CrouchRun = Mixer("Weapon/Knife/ThirdPerson", "CrouchRun8Way",
            KnifeCrouchRun("F", F), KnifeCrouchRun("B", B), KnifeCrouchRun("L", L), KnifeCrouchRun("R", R),
            KnifeCrouchRun("FL", FL), KnifeCrouchRun("FR", FR), KnifeCrouchRun("BL", BL), KnifeCrouchRun("BR", BR));
        tp.Locomotion.LeanLeft = Knife3("LeanLeft", "Melee_M_Knife_3P_Stand_LeanAdditivityL.fbx");
        tp.Locomotion.LeanRight = Knife3("LeanRight", "Melee_M_Knife_3P_Stand_LeanAdditivityR.fbx");
        tp.Combat.Attack = Knife3("Attack", "Melee_M_Knife_3P_montage.fbx");
        tp.Combat.AttackLoop = Knife3("AttackLoop", "Melee_M_Knife_3P_montage_SEQ1.fbx");
        tp.Combat.AlternateAttack = Knife3("AlternateAttack", "Melee_M_Knife_3P_Idle_fire_02.fbx");
        tp.Equipment.Equip = Knife3("Equip", "Melee_M_Knife_3P_Getweapon.fbx");
        tp.Equipment.Unequip = Knife3("Unequip", "Melee_M_Knife_3P_Putweapon.fbx");
        tp.Stance.CrouchIdle = Knife3("CrouchIdle", "Meless_M_Knife_3P_Crouch.fbx");
        tp.Stance.ProneIdle = Knife3("ProneIdle", "Melee_M_Knife_3P_Proneidle.fbx");
        tp.Stance.CrouchAttack = Knife3("CrouchAttack", "Melee_M_Knife_3P_Idle_fire_01.fbx");
        tp.Stance.ProneAttack = Knife3("ProneAttack", "Melee_M_Knife_3P_ProneMontage.fbx");
        tp.Stance.ProneEquip = Knife3("ProneEquip", "Melee_M_Knife_3P_ProneGetweapon.fbx");
        tp.Stance.ProneUnequip = Knife3("ProneUnequip", "Melee_M_Knife_3P_PronePutweapon.fbx");

        EditorUtility.SetDirty(config);
    }

    private static void ConfigureAk12()
    {
        WeaponAnimationSO config = LoadOrCreate<WeaponAnimationSO>(
            $"{ConfigRoot}/WeaponAnimations/AK12.asset");
        config.Weapon = E_Weapon.AK12;
        EnsureWeaponGroups(config);

        FirstPersonWeaponAnimations fp = config.FirstPerson;
        fp.Idle = Ak1("Idle", "AR_M_1P_AK12_Idle01.fbx");
        fp.IdleAction1 = Ak1("IdleAction1", "AR_M_1P_AK12_Idle01_IKpose01.fbx");
        fp.IdleAction2 = Ak1("IdleAction2", "AR_M_1P_AK12_Idle01_IKpose02.fbx");
        fp.Locomotion.SprintOffsetPose = Ak1("SprintOffsetPose", "AR_M_1P_AK12_Sprint_Offset_Pose.fbx");
        fp.Locomotion.SuperSprintOffsetPose = Ak1("SuperSprintOffsetPose", "AR_M_1P_AK12_SuperSprint_Offset_Pose.fbx");
        fp.Airborne.JumpStart = Ak1("JumpStart", "AR_M_1P_AK12_JumpStart.fbx");
        fp.Airborne.JumpLoop = Ak1("JumpLoop", "AR_M_1P_AK12_JumpLoop.fbx");
        fp.Airborne.JumpLand = Ak1("JumpLand", "AR_M_1P_AK12_JumpEnd.fbx");
        fp.Airborne.AimJumpStart = Ak1("AimJumpStart", "AR_M_1P_AK12_AimJumpStart.fbx");
        fp.Airborne.AimJumpLoop = Ak1("AimJumpLoop", "AR_M_1P_AK12_AimJumpLoop.fbx");
        fp.Airborne.AimJumpLand = Ak1("AimJumpLand", "AR_M_1P_AK12_AimJumpEnd.fbx");
        fp.Combat.Attack = Ak1("Attack", "AR_M_1P_AK12_Fire.fbx");
        fp.Combat.AttackLoop = Ak1("AttackLoop", "AR_M_1P_AK12_FireContinue.fbx");
        fp.Combat.AttackEnd = Ak1("AttackEnd", "AR_M_1P_AK12_FireEnd.fbx");
        fp.Combat.AimAttack = Ak1("AimAttack", "AR_M_1P_AK12_Aimfire.fbx");
        fp.Combat.AimAttackLoop = Ak1("AimAttackLoop", "AR_M_1P_AK12_AimfireContinue.fbx");
        fp.Combat.AimAttackEnd = Ak1("AimAttackEnd", "AR_M_1P_AK12_AimingFireEnd.fbx");
        fp.Combat.AimIdle = Ak1("AimIdle", "AR_M_1P_AK12_Aimidle.fbx");
        fp.Combat.AimIdleAdditive = Ak1("AimIdleAdditive", "AR_M_1P_AK12_AimingIdleAdditive.fbx");
        fp.Combat.AimOn = Ak1("AimOn", "AR_M_1P_AK12_AimOn.fbx");
        fp.Combat.AimOff = Ak1("AimOff", "AR_M_1P_AK12_AimOff.fbx");
        fp.Combat.Reload = Ak1("Reload", "AR_M_1P_AK12_ChangeClip.fbx");
        fp.Combat.ReloadEmpty = Ak1("ReloadEmpty", "AR_M_1P_AK12_ChangeClipFull.fbx");
        fp.Combat.AimReload = Ak1("AimReload", "AR_M_1P_AK12_AimChangeClip.fbx");
        fp.Combat.AimReloadEmpty = Ak1("AimReloadEmpty", "AR_M_1P_AK12_AimChangeClipFull_45.fbx");
        fp.Combat.ToSingleFire = Ak1("ToSingleFire", "AR_M_1P_AK12_Changemode_One.fbx");
        fp.Combat.ToAutomaticFire = Ak1("ToAutomaticFire", "AR_M_1P_AK12_Changemode_More.fbx");
        fp.Combat.AimToSingleFire = Ak1("AimToSingleFire", "AR_M_1P_AK12_AimChangemode_One.fbx");
        fp.Combat.AimToAutomaticFire = Ak1("AimToAutomaticFire", "AR_M_1P_AK12_AimChangemode_More.fbx");
        fp.Equipment.EquipInitial = Ak1("EquipInitial", "AR_M_AK12_1P_GetWeaponInitial.fbx");
        fp.Equipment.Equip = Ak1("Equip", "AR_M_1P_AK12_GetWeapon.fbx");
        fp.Equipment.EquipFast = Ak1("EquipFast", "AR_M_1P_AK12_GetWeaponFast.fbx");
        fp.Equipment.Unequip = Ak1("Unequip", "AR_M_1P_AK12_PutWeapon.fbx");
        fp.Equipment.UnequipFast = Ak1("UnequipFast", "AR_M_1P_AK12_PutWeaponFast.fbx");
        fp.Equipment.Inspect = Ak1("Inspect", "AR_M_1P_AK12_Inspect.fbx");
        fp.Equipment.InspectEmpty = Ak1("InspectEmpty", "AR_M_1P_AK12_InspectEmpty.fbx");

        ThirdPersonUpperBodyAnimations tp = config.ThirdPersonUpperBody;
        tp.Idle = Rifle3("Idle", "JogWalk/Idle/R_Idle.fbx");
        tp.Locomotion.Walk = RifleMixer("Walk8Way", "JogWalk/StrafeWalk", "R_StrafeWalk");
        tp.Locomotion.Jog = RifleMixer("Jog8Way", "JogWalk/StrafeJog", "R_StrafeJog");
        tp.Locomotion.Run = RifleMixer("Run8Way", "runSprint/StrafeRun", "R_StrafeRun");
        tp.Locomotion.Sprint = RifleMixer("Sprint8Way", "runSprint/StrafeSprint", "R_StrafeSprint");
        tp.Locomotion.LeanLeft = Rifle3("LeanLeft", "JogWalk/Walk/R_Walk_LeanL.fbx");
        tp.Locomotion.LeanRight = Rifle3("LeanRight", "JogWalk/Walk/R_Walk_LeanR.fbx");
        tp.Combat.Attack = Existing("Assets/Animation/持枪少女/Upper/Fire.asset");
        tp.Combat.AimIdle = Rifle3("AimIdle", "JogWalk/Idle/R_AimIdle.fbx");
        tp.Combat.AimAttack = tp.Combat.Attack;
        tp.Equipment.Equip = Rifle3("Equip", "Jumps/Jump_Cliff/R_Equip.fbx");
        tp.Equipment.Unequip = Rifle3("Unequip", "Jumps/Jump_Cliff/R_Unequip.fbx");

        EditorUtility.SetDirty(config);
    }

    private static void EnsureWeaponGroups(WeaponAnimationSO config)
    {
        config.FirstPerson ??= new();
        config.FirstPerson.Locomotion ??= new();
        config.FirstPerson.Airborne ??= new();
        config.FirstPerson.Stance ??= new();
        config.FirstPerson.Combat ??= new();
        config.FirstPerson.Equipment ??= new();
        config.ThirdPersonUpperBody ??= new();
        config.ThirdPersonUpperBody.Locomotion ??= new();
        config.ThirdPersonUpperBody.Combat ??= new();
        config.ThirdPersonUpperBody.Equipment ??= new();
        config.ThirdPersonUpperBody.Stance ??= new();
    }

    private static TransitionAsset Knife1(string name, string file)
        => Clip("Weapon/Knife/FirstPerson", name, $"{Knife1P}/{file}");

    private static TransitionAsset Knife3(string name, string file)
        => Clip("Weapon/Knife/ThirdPerson", name, $"{Knife3P}/{file}");

    private static TransitionAsset Ak1(string name, string file)
        => Clip("Weapon/AK12/FirstPerson", name, $"{Ak121P}/{file}");

    private static TransitionAsset Rifle3(string name, string relativePath)
        => Clip("Weapon/AK12/ThirdPerson", name, $"{Rifle3P}/{relativePath}");

    private static DirectionalSource KnifeRunStart(string direction, Vector2 threshold)
        => D($"{Knife3P}/Meless_M_Knife_3P_RunStart{direction}.fbx", threshold);

    private static DirectionalSource KnifeRun(string direction, Vector2 threshold)
        => D($"{Knife3P}/Meless_M_Knife_3P_run{direction}.fbx", threshold);

    private static DirectionalSource KnifeRunStop(string direction, string suffix, Vector2 threshold)
        => D($"{Knife3P}/Meless_M_Knife_3P_RunStop{direction}{suffix}.fbx", threshold);

    private static DirectionalSource KnifeWalk(string direction, Vector2 threshold)
        => D($"{Knife3P}/Melee_M_Knife_3P_Walk{direction}.fbx", threshold);

    private static DirectionalSource KnifeCrouchWalk(string direction, Vector2 threshold)
        => D($"{Knife3P}/Melee_M_Knife_3P_Crouch_Walk{direction}.fbx", threshold);

    private static DirectionalSource KnifeCrouchRun(string direction, Vector2 threshold)
        => D($"{Knife3P}/Melee_M_Knife_3P_Crouch_run{direction}.fbx", threshold);

    private static DirectionalSource KnifeSupineRun(string direction, Vector2 threshold)
        => D($"{Knife3P}/supine/Meless_M_Knife_3P_SupineRun{direction}.fbx", threshold);

    private static DirectionalSource UnarmedCrouchWalk(string direction, Vector2 threshold)
        => D($"{Unarmed3P}/Normal/Unarmed_M_3P_CrouchWalk{direction}.fbx", threshold);

    private static DirectionalSource UnarmedProne(string direction, Vector2 threshold)
        => D($"{Unarmed3P}/Normal/Unarmed_M_3P_Prone{direction}.fbx", threshold);

    private static DirectionalSource Injured(string speed, string direction, Vector2 threshold)
        => D($"{Unarmed3P}/3p_injure/Unarmed_M_3P_Injured{speed}{direction}_knife.fbx", threshold);

    private static TransitionAsset KnifeHit(string suffix, string stance, string direction)
        => Clip($"FullBody/Hit/{stance}", direction,
            $"{Knife3P}/hit/Knife_M_3P_Hit_{suffix}.fbx");

    private static TransitionAsset RifleMixer(string name, string folder, string prefix)
        => Mixer("Weapon/AK12/ThirdPerson", name,
            D($"{Rifle3P}/{folder}/{prefix}_F.fbx", F),
            D($"{Rifle3P}/{folder}/{prefix}_B.fbx", B),
            D($"{Rifle3P}/{folder}/{prefix}_L.fbx", L),
            D($"{Rifle3P}/{folder}/{prefix}_R.fbx", R),
            D($"{Rifle3P}/{folder}/{prefix}_L45.fbx", FL),
            D($"{Rifle3P}/{folder}/{prefix}_R45.fbx", FR),
            D($"{Rifle3P}/{folder}/{prefix}_L135.fbx", BL),
            D($"{Rifle3P}/{folder}/{prefix}_R135.fbx", BR));

    private static DirectionalSource D(string path, Vector2 threshold)
        => new(path, threshold);

    private static TransitionAsset Clip(string group, string name, string sourcePath)
    {
        AnimationClip clip = LoadClip(sourcePath);
        if (clip == null)
            return null;

        string folder = $"{GeneratedRoot}/{group}";
        EnsureFolder(folder);
        string assetPath = $"{folder}/{Sanitize(name)}.asset";
        TransitionAsset asset = AssetDatabase.LoadAssetAtPath<TransitionAsset>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<TransitionAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.name = name;
        asset.Transition = new ClipTransition { Clip = clip };
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static TransitionAsset Mixer(string group, string name, params DirectionalSource[] sources)
    {
        var clips = new List<Object>();
        var thresholds = new List<Vector2>();

        foreach (DirectionalSource source in sources)
        {
            AnimationClip clip = LoadClip(source.Path);
            if (clip == null)
                continue;
            clips.Add(clip);
            thresholds.Add(source.Threshold);
        }

        if (clips.Count == 0)
        {
            Warnings.Add($"Mixer '{name}' has no valid clips");
            return null;
        }

        string folder = $"{GeneratedRoot}/{group}";
        EnsureFolder(folder);
        string assetPath = $"{folder}/{Sanitize(name)}.asset";
        TransitionAsset asset = AssetDatabase.LoadAssetAtPath<TransitionAsset>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<TransitionAsset>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }

        asset.name = name;
        asset.Transition = new MixerTransition2D
        {
            Animations = clips.ToArray(),
            Thresholds = thresholds.ToArray(),
            Type = MixerTransition2D.MixerType.Directional,
        };
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static AnimationClip LoadClip(string assetPath)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(candidate => !candidate.name.StartsWith("__preview__", StringComparison.Ordinal));

        if (clip == null)
            Warnings.Add($"Missing clip: {assetPath}");
        return clip;
    }

    private static TransitionAsset Existing(string assetPath)
    {
        TransitionAsset asset = AssetDatabase.LoadAssetAtPath<TransitionAsset>(assetPath);
        if (asset == null)
            Warnings.Add($"Missing transition: {assetPath}");
        return asset;
    }

    private static T LoadOrCreate<T>(string assetPath) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset != null)
            return asset;

        EnsureFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }

    private static string Sanitize(string name)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            name = name.Replace(invalid, '_');
        return name;
    }

    private readonly struct DirectionalSource
    {
        public readonly string Path;
        public readonly Vector2 Threshold;

        public DirectionalSource(string path, Vector2 threshold)
        {
            Path = path;
            Threshold = threshold;
        }
    }
}
