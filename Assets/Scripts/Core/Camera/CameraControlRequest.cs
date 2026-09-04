public readonly struct CameraControlRequest
{
    public readonly bool DisableRotation;

    public CameraControlRequest(bool disableRotation)
    {
        DisableRotation=disableRotation;
    }

    public static CameraControlRequest DisableAll=>new(true);
}
