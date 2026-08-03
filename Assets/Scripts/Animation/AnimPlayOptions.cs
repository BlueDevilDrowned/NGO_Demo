using System;

[Serializable]
public struct AnimPlayOptions
{
    public float FadeDuration;
    public float Speed;
    public float NormalizedTime;
    public static AnimPlayOptions Default=>new()
    {
        FadeDuration=-1f,
        Speed=1f,
        NormalizedTime=-1f,
    };
}
