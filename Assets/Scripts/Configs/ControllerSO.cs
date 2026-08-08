using UnityEngine;

[CreateAssetMenu(fileName = "ControllerSO", menuName = "Scriptable Objects/ControllerSO")]
public class ControllerSO : ScriptableObject
{
    public float WalkSpeed=2f;
    public float WalkmaxRotation=180;
    public float JogSpeed=3f;
    public float JogmaxRotation=270;
    public float SprintSpeed;
    public float AimWalkSpeed=3;

    public float JumpVelocity=10f;
    public float JumpMaxRotation=180;
    [Tooltip("跳跃水平速度")]
    public float JumpSpeed=3f;
    [Header("Gravite")]
    public float Gravite=-20;
    public float GroundedVelocity=-1;
    public float UpFactor=0.5f;
    public float FallFactor=2f;
    public float HoldFactor=0.1f;
    public float MaxfallSpeed=-20f;
    public float HoldSpeed=0.5f;

    [Header("Landing Impact Speed Grades")]
    [Tooltip("Minimum downward impact speed for Land_2h.")]
    [Min(0f)]public float Land2MinImpactSpeed=6f;
    [Tooltip("Minimum downward impact speed for Land_3h.")]
    [Min(0f)]public float Land3MinImpactSpeed=10f;
    [Tooltip("Minimum downward impact speed for Land_4h.")]
    [Min(0f)]public float Land4MinImpactSpeed=15f;
    [Header("RunLand Rotation By Impact Level")]
    [Tooltip("Maximum Level 1 RunLand rotation speed in degrees per second.")]
    [Min(0f)]public float Land1MaxRotation=180f;
    [Tooltip("Maximum Level 2 RunLand rotation speed in degrees per second.")]
    [Min(0f)]public float Land2MaxRotation=180f;
    [Tooltip("Maximum Level 3 RunLand rotation speed in degrees per second.")]
    [Min(0f)]public float Land3MaxRotation=180f;
    [Tooltip("Maximum Level 4 RunLand rotation speed in degrees per second.")]
    [Min(0f)]public float Land4MaxRotation=180f;

    public LandingImpactLevel GetLandingImpactLevel(float impactSpeed)
    {
        float level2=Mathf.Max(0f,Land2MinImpactSpeed);
        float level3=Mathf.Max(level2,Land3MinImpactSpeed);
        float level4=Mathf.Max(level3,Land4MinImpactSpeed);

        if(impactSpeed>=level4)return LandingImpactLevel.Level4;
        if(impactSpeed>=level3)return LandingImpactLevel.Level3;
        if(impactSpeed>=level2)return LandingImpactLevel.Level2;
        return LandingImpactLevel.Level1;
    }

    public float GetLandingMaxRotation(LandingImpactLevel level)
    {
        return level switch
        {
            LandingImpactLevel.Level4=>Land4MaxRotation,
            LandingImpactLevel.Level3=>Land3MaxRotation,
            LandingImpactLevel.Level2=>Land2MaxRotation,
            _=>Land1MaxRotation,
        };
    }
}

public enum LandingImpactLevel
{
    Level1=1,
    Level2=2,
    Level3=3,
    Level4=4,
}
