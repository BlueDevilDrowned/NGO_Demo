using System.Collections.Generic;
using UnityEngine;

public static class LagCompensationWorld
{
    private static readonly HashSet<LagCompensatedBody> bodies = new();
    private static LagCompensationConfig config;
    private static LagCompensationSystem system;

    public static IReadOnlyCollection<LagCompensatedBody> Bodies => bodies;
    public static LagCompensationConfig Config => config;
    public static LagCompensationSystem System => system;
    public static float CaptureIntervalSeconds => config != null
        ? config.CaptureIntervalSeconds
        : 1f / 60f;

    public static int GetCaptureIntervalTicks(uint tickRate)
    {
        return config != null
            ? config.GetCaptureIntervalTicks(tickRate)
            : Mathf.Max(1, Mathf.RoundToInt(CaptureIntervalSeconds * tickRate));
    }

    public static void Configure(LagCompensationConfig value)
    {
        config = value;
    }

    public static bool TrySetSystem(LagCompensationSystem value)
    {
        if (system != null && system != value)
            return false;

        system = value;
        return true;
    }

    public static void ClearSystem(LagCompensationSystem value)
    {
        if (system == value)
            system = null;
    }

    public static void ClearConfiguration(LagCompensationConfig value)
    {
        if (config == value)
            config = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        bodies.Clear();
        config = null;
        system = null;
    }

    public static void TryRegister(LagCompensatedBody body)
    {
        if (body == null || !bodies.Add(body)) return;
        system?.Register(body);
    }

    public static void TryUnregister(LagCompensatedBody body)
    {
        if (body == null || !bodies.Remove(body)) return;
        system?.Unregister(body);
    }
}
