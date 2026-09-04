public readonly struct CameraRotationRequest
{
    public readonly string Source;
    public readonly float YawDelta;
    public readonly float PitchDelta;

    public CameraRotationRequest(
        string source,
        float yawDelta,
        float pitchDelta)
    {
        Source=source;
        YawDelta=yawDelta;
        PitchDelta=pitchDelta;
    }
}
