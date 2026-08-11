public readonly struct AnimationControlRequest
{
    public readonly bool DisableAnimator;

    public AnimationControlRequest(bool disableAnimator)
    {
        DisableAnimator=disableAnimator;
    }

    public static AnimationControlRequest Disable=>new(true);
}
