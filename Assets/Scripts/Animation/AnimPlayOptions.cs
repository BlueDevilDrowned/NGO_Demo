using System;

[Serializable]
public struct AnimPlayOptions
{
    public int Layer;
    public float FadeDuration;
    public float Speed;
    public float NormalizedTime;
    public static AnimPlayOptions Default=>new()
    {
        Layer=0,
        FadeDuration=-1f,
        Speed=1f,
        NormalizedTime=-1f,
    };
}
