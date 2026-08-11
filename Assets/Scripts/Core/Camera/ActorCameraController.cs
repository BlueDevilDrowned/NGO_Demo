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
    private Transform collisionIgnoreRoot;
    private CinemachineInputAxisController freeLookInput;
    private CinemachineThirdPersonAim thirdPersonAim;
    private readonly RaycastHit[] aimHits=new RaycastHit[64];
    private LayerMask aimCollisionMask;
    private float aimDistance=200f;
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

    public void Bind(
        Transform cameraTarget,
        Transform ignoreCollisionRoot,
        AimSO aimConfig)
    {
        if (cameraTarget == null)
        {
            Debug.LogError("Cannot bind cameras without a camera target.", this);
            return;
        }

        boundTarget = cameraTarget;
        collisionIgnoreRoot=ignoreCollisionRoot;
        SetTrackingTarget(freeCamera, cameraTarget);
        SetTrackingTarget(aimCamera, cameraTarget);
        if(aimConfig!=null)
        {
            aimCollisionMask=aimConfig.TargetCollisionMask;
            aimDistance=Mathf.Max(1f,aimConfig.TargetDistance);
        }
        if(thirdPersonAim!=null)
        {
            thirdPersonAim.AimCollisionFilter=0;
            thirdPersonAim.AimDistance=aimDistance;
        }
        SetAimMode(false);
    }

    public void Unbind(Transform cameraTarget)
    {
        if (cameraTarget == null || cameraTarget != boundTarget)
            return;

        SetTrackingTarget(freeCamera, null);
        SetTrackingTarget(aimCamera, null);
        boundTarget = null;
        collisionIgnoreRoot=null;
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

        if(liveCamera!=aimCamera||outputCamera==null||boundTarget==null)return;

        latestAimTarget=ResolveAimTarget();
        hasAimTarget=true;
        AimTargetUpdated?.Invoke(latestAimTarget);
    }

    private Vector3 ResolveAimTarget()
    {
        Transform cameraTransform=outputCamera.transform;
        Vector3 cameraOrigin=cameraTransform.position;
        Vector3 cameraDirection=cameraTransform.forward;
        Vector3 cameraTarget=cameraOrigin+cameraDirection*aimDistance;

        if(TryGetClosestValidHit(
               cameraOrigin,
               cameraDirection,
               aimDistance,
               out RaycastHit cameraHit))
            cameraTarget=cameraHit.point;

        Vector3 actorOrigin=boundTarget.position;
        Vector3 actorToTarget=cameraTarget-actorOrigin;
        float actorRayDistance=actorToTarget.magnitude;
        if(actorRayDistance>Mathf.Epsilon&&
           TryGetClosestValidHit(
               actorOrigin,
               actorToTarget/actorRayDistance,
               actorRayDistance,
               out RaycastHit actorHit))
            return actorHit.point;

        return cameraTarget;
    }

    private bool TryGetClosestValidHit(
        Vector3 origin,
        Vector3 direction,
        float distance,
        out RaycastHit closestHit)
    {
        int hitCount=Physics.RaycastNonAlloc(
            origin,
            direction,
            aimHits,
            distance,
            aimCollisionMask,
            QueryTriggerInteraction.Collide);
        closestHit=default;
        float closestDistance=float.PositiveInfinity;

        for(int i=0;i<hitCount;i++)
        {
            RaycastHit candidate=aimHits[i];
            if(IsOwnedCollider(candidate.collider)||
               candidate.distance>=closestDistance)continue;

            closestDistance=candidate.distance;
            closestHit=candidate;
        }

        return !float.IsPositiveInfinity(closestDistance);
    }

    private bool IsOwnedCollider(Collider candidate)
    {
        if(candidate==null)return false;

        Transform candidateTransform=candidate.transform;
        if(collisionIgnoreRoot!=null&&
           (candidateTransform==collisionIgnoreRoot||
            candidateTransform.IsChildOf(collisionIgnoreRoot)))return true;

        return candidate.TryGetComponent(out Hitbox hitbox)&&
               hitbox.Manager!=null&&
               hitbox.Manager.Owner!=null&&
               hitbox.Manager.Owner.transform==collisionIgnoreRoot;
    }
}
