using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CameraArbiter
{
    private readonly List<CameraRotationRequest> rotationRequests=new();
    private readonly Dictionary<object,CameraControlRequest> controlRequests=new();

    public void Submit(in CameraRotationRequest request)
    {
        if(!IsFinite(request.YawDelta)||!IsFinite(request.PitchDelta))
            return;

        rotationRequests.Add(request);
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

    public void Resolve(
        ref ActorCameraData data,
        CameraSO configuration)
    {
        if(configuration==null)
        {
            rotationRequests.Clear();
            return;
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
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
