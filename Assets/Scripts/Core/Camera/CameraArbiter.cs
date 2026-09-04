using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CameraArbiter
{
    private readonly List<CameraRotationRequest> rotationRequests=new();
    private readonly Dictionary<object,CameraControlRequest> controlRequests=new();
    private Vector2 recoilVelocity;
    private Vector2 recoilOffset;
    private Vector2 recoilRecoveryVelocity;

    public void Submit(in CameraRotationRequest request)
    {
        if(!IsFinite(request.YawDelta)||!IsFinite(request.PitchDelta))
            return;

        rotationRequests.Add(request);
    }

    public void SubmitRecoil(in CameraRecoilRequest request)
    {
        if(!IsFinite(request.YawVelocity)||!IsFinite(request.PitchVelocity))
            return;

        recoilVelocity+=new Vector2(request.YawVelocity,request.PitchVelocity);
    }

    public void SubmitControlRequest(
        object requester,
        in CameraControlRequest request)
    {
        if(requester==null)
            throw new ArgumentNullException(nameof(requester));

        controlRequests[requester]=request;
    }

    public bool RemoveControlRequest(object requester)
    {
        return requester!=null&&controlRequests.Remove(requester);
    }

    public ActorCameraData Resolve(
        ref ActorCameraData data,
        CameraSO configuration,
        float deltaTime)
    {
        if(configuration==null)
        {
            rotationRequests.Clear();
            return data;
        }

        bool rotationDisabled=false;
        foreach(CameraControlRequest request in controlRequests.Values)
            rotationDisabled|=request.DisableRotation;

        if(!rotationDisabled)
        {
            float yawDelta=0f;
            float pitchDelta=0f;
            for(int i=0;i<rotationRequests.Count;i++)
            {
                CameraRotationRequest request=rotationRequests[i];
                yawDelta+=request.YawDelta;
                pitchDelta+=request.PitchDelta;
            }

            data.ViewYaw=Mathf.Repeat(data.ViewYaw+yawDelta,360f);
            data.ViewPitch=Mathf.Clamp(
                data.ViewPitch+pitchDelta,
                configuration.FirstPersonMinPitch,
                configuration.FirstPersonMaxPitch);
        }

        rotationRequests.Clear();
        UpdateRecoil(configuration,deltaTime);

        ActorCameraData applied=data;
        applied.ViewYaw=Mathf.Repeat(applied.ViewYaw+recoilOffset.x,360f);
        applied.ViewPitch=Mathf.Clamp(
            applied.ViewPitch+recoilOffset.y,
            configuration.FirstPersonMinPitch,
            configuration.FirstPersonMaxPitch);
        return applied;
    }

    private void UpdateRecoil(CameraSO configuration,float deltaTime)
    {
        if(deltaTime<=0f)return;

        recoilOffset+=recoilVelocity*deltaTime;
        recoilVelocity=Vector2.MoveTowards(
            recoilVelocity,
            Vector2.zero,
            configuration.RecoilVelocityDamping*deltaTime);

        float threshold=Mathf.Max(0f,configuration.RecoilVelocityThreshold);
        if(recoilVelocity.sqrMagnitude<=threshold*threshold)
        {
            recoilOffset=Vector2.SmoothDamp(
                recoilOffset,
                Vector2.zero,
                ref recoilRecoveryVelocity,
                Mathf.Max(0.01f,configuration.RecoilRecoverySmoothTime),
                Mathf.Infinity,
                deltaTime);
        }
        else
        {
            recoilRecoveryVelocity=Vector2.zero;
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
