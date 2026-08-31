using Unity.Cinemachine;
using UnityEngine;

public sealed class ActorCameraRig : MonoBehaviour
{
    public static ActorCameraRig Instance { get; private set; }

    [SerializeField] private Camera outputCamera;
    [SerializeField] private CinemachineCamera freeCamera;
    [SerializeField] private CinemachineCamera aimCamera;
    [SerializeField] private CinemachineCamera firstPersonCamera;
    [SerializeField] private string firstPersonHiddenLayerName=
        "LocalFirstPersonHidden";

    [SerializeField] private int inactivePriority;
    [SerializeField] private int activePriority = 20;

    private Transform boundPivot;
    private Transform firstPersonTarget;
    private CameraPerspectiveMode perspectiveMode;
    private CameraViewMode mode;
    private bool hasPerspectiveMode;
    private bool hasMode;

    public Transform OutputTransform =>
        outputCamera != null ? outputCamera.transform : null;

    public bool IsBoundTo(Transform target)
    {
        return target!=null&&boundPivot==target;
    }

    public bool IsFirstPersonTarget(Transform target)
    {
        return target!=null&&firstPersonTarget==target;
    }

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }

        Instance = this;
        ConfigureOutputCameraCulling();
        SetPerspectiveMode(CameraPerspectiveMode.ThirdPerson);
        SetViewMode(CameraViewMode.FreeLook);
    }

    public void Bind(Transform cameraPivot)
    {
        boundPivot = cameraPivot;

        SetTrackingTarget(freeCamera, cameraPivot);
        SetTrackingTarget(aimCamera, cameraPivot);
    }

    public void SetFirstPersonTarget(Transform target)
    {
        firstPersonTarget=target;
        SetTrackingTarget(firstPersonCamera, target);
    }
    public void Unbind(Transform cameraPivot)
    {
        if(cameraPivot == null || cameraPivot != boundPivot)
            return;

        SetTrackingTarget(freeCamera, null);
        SetTrackingTarget(aimCamera, null);
        SetTrackingTarget(firstPersonCamera, null);

        boundPivot = null;
        firstPersonTarget = null;
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
        Quaternion viewRotation=Quaternion.Euler(
            data.ViewPitch,
            data.ViewYaw,
            0f);

        // 两套相机共享同一个逻辑视角，透视模式只选择最终画面。
        if(boundPivot!=null)
            boundPivot.rotation=viewRotation;
        if(firstPersonTarget!=null&&firstPersonTarget!=boundPivot)
            firstPersonTarget.rotation=viewRotation;
    }
    public void SetViewMode(CameraViewMode nextMode)
    {
        if(hasMode&&mode == nextMode)
            return;

        mode = nextMode;
        hasMode=true;

        RefreshCameraPriorities();
    }

    public void SetPerspectiveMode(CameraPerspectiveMode nextMode)
    {
        if(hasPerspectiveMode&&perspectiveMode==nextMode)
            return;

        perspectiveMode=nextMode;
        hasPerspectiveMode=true;

        RefreshCameraPriorities();
    }

    private void RefreshCameraPriorities()
    {
        bool firstPerson =
            perspectiveMode == CameraPerspectiveMode.FirstPerson;
        bool freeLook = !firstPerson&&mode == CameraViewMode.FreeLook;
        bool aiming = !firstPerson&&mode == CameraViewMode.Aim;

        if(freeCamera != null)
            freeCamera.Priority =
                freeLook ? activePriority : inactivePriority;

        if(aimCamera != null)
            aimCamera.Priority =
                aiming ? activePriority : inactivePriority;

        if(firstPersonCamera != null)
            firstPersonCamera.Priority =
                firstPerson ? activePriority : inactivePriority;
    }

    private void ConfigureOutputCameraCulling()
    {
        if(outputCamera==null)return;

        int hiddenLayer=LayerMask.NameToLayer(firstPersonHiddenLayerName);
        if(hiddenLayer<0)
        {
            Debug.LogError(
                $"Layer is not configured: {firstPersonHiddenLayerName}",
                this);
            return;
        }

        outputCamera.cullingMask&=~(1<<hiddenLayer);
    }

    private void OnDestroy()
    {
        if(Instance == this)
            Instance = null;
    }
}
