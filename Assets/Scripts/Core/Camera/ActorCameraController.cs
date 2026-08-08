using System;
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
    private CinemachineInputAxisController freeLookInput;
    private CinemachineThirdPersonAim thirdPersonAim;
    private bool hasAimTarget;
    private Vector3 latestAimTarget;

    public Transform OutputTransform => outputCamera != null ? outputCamera.transform : null;
    public bool IsAiming { get; private set; }
    public event Action<Vector3> AimTargetUpdated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Only one ActorCameraController can be active.", this);
            enabled = false;
            return;
        }

        Instance = this;
        if (freeCamera != null)
            freeCamera.TryGetComponent(out freeLookInput);
        if (aimCamera != null)
            aimCamera.TryGetComponent(out thirdPersonAim);
        SetAimMode(false);
    }

    private void OnDestroy()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
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
        if(!isAiming)
            hasAimTarget=false;

        if (freeCamera != null)
            freeCamera.Priority = isAiming ? inactivePriority : activePriority;

        if (aimCamera != null)
            aimCamera.Priority = isAiming ? activePriority : inactivePriority;

        if (freeLookInput != null)
            freeLookInput.enabled = !isAiming;
    }

    public bool TryGetAimTarget(out Vector3 target)
    {
        target=latestAimTarget;
        return IsAiming&&hasAimTarget;
    }

    private static void SetTrackingTarget(CinemachineCamera camera, Transform target)
    {
        if (camera == null)
            return;

        camera.Target.TrackingTarget = target;
        camera.Target.CustomLookAtTarget = false;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x)&&!float.IsInfinity(value.x)&&
               !float.IsNaN(value.y)&&!float.IsInfinity(value.y)&&
               !float.IsNaN(value.z)&&!float.IsInfinity(value.z);
    }

    private void OnCameraUpdated(CinemachineBrain brain)
    {
        if(!IsAiming||brain==null||brain.OutputCamera!=outputCamera||
           thirdPersonAim==null||!thirdPersonAim.enabled)return;

        CinemachineCamera liveCamera;
        if(brain.ActiveVirtualCamera is CinemachineCameraManagerBase manager)
            liveCamera=manager.LiveChild as CinemachineCamera;
        else
            liveCamera=brain.ActiveVirtualCamera as CinemachineCamera;

        if(liveCamera!=aimCamera||!IsFinite(thirdPersonAim.AimTarget))return;

        latestAimTarget=thirdPersonAim.AimTarget;
        hasAimTarget=true;
        AimTargetUpdated?.Invoke(latestAimTarget);
    }
}
