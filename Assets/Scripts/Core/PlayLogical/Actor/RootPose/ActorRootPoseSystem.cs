using System;
using UnityEngine;

public sealed class ActorRootPoseSystem:IActorSystem
{
    private const float RemoteSmoothTime=0.08f;

    private readonly Actor actor;
    private readonly Transform presentationRoot;
    private readonly Quaternion presentationRotationOffset;
    private readonly ActorRootPoseReplication replication;

    private float authoritativeYaw;
    private float presentationYaw;
    private float remoteTargetYaw;
    private float remoteYawVelocity;
    private bool isDisposed;

    public ActorRootPose AuthoritativePose=>CreatePose(authoritativeYaw);
    public ActorRootPose PresentationPose=>CreatePose(presentationYaw);
    public Vector3 AuthoritativeForward=>AuthoritativePose.Forward;

    public Vector3 InverseTransformAuthoritativeDirection(Vector3 direction)
    {
        return Quaternion.Inverse(AuthoritativePose.Rotation)*direction;
    }

    public Vector3 InverseTransformPresentationDirection(Vector3 direction)
    {
        return Quaternion.Inverse(PresentationPose.Rotation)*direction;
    }

    public ActorRootPoseSystem(Actor actor,Transform presentationRoot)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        this.presentationRoot=presentationRoot;

        authoritativeYaw=actor.transform.eulerAngles.y;
        presentationYaw=authoritativeYaw;
        remoteTargetYaw=authoritativeYaw;
        Quaternion initialRootRotation=Quaternion.Euler(
            0f,
            authoritativeYaw,
            0f);
        presentationRotationOffset=presentationRoot!=null
            ?Quaternion.Inverse(initialRootRotation)*presentationRoot.rotation
            :Quaternion.identity;

        replication=new ActorRootPoseReplication(actor,authoritativeYaw);
        actor.RegisterSystem(this);
    }

    public void SetAuthoritativeYaw(float yaw)
    {
        if(!actor.IsServer||!ActorCameraDataUtility.IsFinite(yaw))
            return;

        authoritativeYaw=Mathf.Repeat(yaw,360f);
        remoteTargetYaw=authoritativeYaw;
        replication.MarkAuthoritativeYaw(authoritativeYaw);
    }

    public void RotateAuthoritative(float yawDelta)
    {
        if(!ActorCameraDataUtility.IsFinite(yawDelta)||
           Mathf.Abs(yawDelta)<=Mathf.Epsilon)
            return;

        SetAuthoritativeYaw(authoritativeYaw+yawDelta);
    }

    public void PresentationUpdate(float deltaTime)
    {
        if(replication.TrySamplePresentationState(
               actor.localTick,
               out ActorRootPoseSnapshot snapshot))
            remoteTargetYaw=snapshot.Yaw;

        if(actor.IsOwner&&actor.cameraSystem!=null&&
           ActorCameraDataUtility.IsFinite(actor.cameraSystem.data.ViewYaw))
        {
            presentationYaw=Mathf.Repeat(
                actor.cameraSystem.data.ViewYaw,
                360f);
        }
        else if(actor.IsClient)
        {
            presentationYaw=Mathf.SmoothDampAngle(
                presentationYaw,
                remoteTargetYaw,
                ref remoteYawVelocity,
                RemoteSmoothTime,
                Mathf.Infinity,
                Mathf.Max(0f,deltaTime));
        }
        else
        {
            presentationYaw=authoritativeYaw;
        }

        if(presentationRoot!=null)
            presentationRoot.rotation=
                Quaternion.Euler(0f,presentationYaw,0f)*
                presentationRotationOffset;
    }

    public void Dispose()
    {
        if(isDisposed)return;

        isDisposed=true;
        replication.Dispose();
    }

    private ActorRootPose CreatePose(float yaw)
    {
        Transform root=actor.transform;
        return new ActorRootPose(
            root.position,
            Quaternion.Euler(0f,yaw,0f),
            root.lossyScale);
    }
}
