public readonly struct CameraRecoilRequest
{
    public readonly string Source;
    public readonly float YawVelocity;
    public readonly float PitchVelocity;

    public CameraRecoilRequest(
        string source,
        float yawVelocity,
        float pitchVelocity)
    {
        Source=source;
        YawVelocity=yawVelocity;
        PitchVelocity=pitchVelocity;
    }
}
