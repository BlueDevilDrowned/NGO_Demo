using Unity.Cinemachine;
using UnityEngine;

public sealed class ActorCameraRig : MonoBehaviour
{
    public static ActorCameraRig Instance { get; private set; }

    [SerializeField] private Camera outputCamera;
    [SerializeField] private CinemachineCamera freeCamera;
    [SerializeField] private CinemachineCamera aimCamera;

    [SerializeField] private int inactivePriority;
    [SerializeField] private int activePriority = 20;

    private Transform boundPivot;
    private CameraViewMode mode;
    private bool hasMode;

    public Transform OutputTransform =>
        outputCamera != null ? outputCamera.transform : null;

    public bool IsBoundTo(Transform target)
    {
        return target!=null&&boundPivot==target;
    }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        SetViewMode(CameraViewMode.FreeLook);
    }

    public void Bind(Transform cameraPivot)
    {
        boundPivot = cameraPivot;

        SetTrackingTarget(freeCamera, cameraPivot);
        SetTrackingTarget(aimCamera, cameraPivot);
    }
    public void Unbind(Transform cameraPivot)
    {
        if(cameraPivot == null || cameraPivot != boundPivot)
            return;

        SetTrackingTarget(freeCamera, null);
        SetTrackingTarget(aimCamera, null);

        boundPivot = null;
    }
    private static void SetTrackingTarget(
    CinemachineCamera camera,
    Transform target)
    {
        if(camera == null)
            return;

        camera.Target.TrackingTarget = target;
        camera.Target.CustomLookAtTarget = false;
    }
    public void ApplyView(in ActorCameraData data)
    {
        if(boundPivot == null)
            return;

        boundPivot.rotation = Quaternion.Euler(
            data.ViewPitch,
            data.ViewYaw,
            0f);
    }
    public void SetViewMode(CameraViewMode nextMode)
    {
        if(hasMode&&mode == nextMode)
            return;

        mode = nextMode;
        hasMode=true;

        bool aiming = mode == CameraViewMode.Aim;

        if(freeCamera != null)
            freeCamera.Priority =
                aiming ? inactivePriority : activePriority;

        if(aimCamera != null)
            aimCamera.Priority =
                aiming ? activePriority : inactivePriority;
    }

    private void OnDestroy()
    {
        if(Instance == this)
            Instance = null;
    }
}
