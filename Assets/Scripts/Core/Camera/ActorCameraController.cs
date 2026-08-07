using Unity.Cinemachine;
using UnityEngine;

public sealed class ActorCameraController : MonoBehaviour
{
    public static ActorCameraController Instance { get; private set; }

    [SerializeField] private Camera outputCamera;
    [SerializeField] private CinemachineCamera freeCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private int inactivePriority;
    [SerializeField] private int activePriority = 20;

    private Transform boundTarget;

    public Transform OutputTransform => outputCamera != null ? outputCamera.transform : null;
    public bool IsAiming { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one ActorCameraController can be active.", this);
            enabled = false;
            return;
        }

        Instance = this;
        SetAimMode(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Bind(Transform cameraTarget)
    {
        if (cameraTarget == null)
        {
            Debug.LogError("Cannot bind cameras without a camera target.", this);
            return;
        }

        boundTarget = cameraTarget;
        SetTrackingTarget(freeCamera, cameraTarget);
        SetTrackingTarget(aimCamera, cameraTarget);
        SetAimMode(false);
    }

    public void Unbind(Transform cameraTarget)
    {
        if (cameraTarget == null || cameraTarget != boundTarget)
            return;

        SetTrackingTarget(freeCamera, null);
        SetTrackingTarget(aimCamera, null);
        boundTarget = null;
        SetAimMode(false);
    }

    public void SetAimMode(bool isAiming)
    {
        IsAiming = isAiming;

        if (freeCamera != null)
            freeCamera.Priority = isAiming ? inactivePriority : activePriority;

        if (aimCamera != null)
            aimCamera.Priority = isAiming ? activePriority : inactivePriority;
    }

    private static void SetTrackingTarget(CinemachineCamera camera, Transform target)
    {
        if (camera == null)
            return;

        camera.Target.TrackingTarget = target;
        camera.Target.CustomLookAtTarget = false;
    }
}
