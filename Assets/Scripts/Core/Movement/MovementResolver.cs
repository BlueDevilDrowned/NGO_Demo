using System.Collections.Generic;
using UnityEngine;

public class MovementResolver
{
    public float ResolveVerticalVelocity(
        IReadOnlyList<MovementRequest>requests,
        float currentVelocity)
    {
        int highestPriority=int.MinValue;
        bool hasVelocityRequest=false;

        for(int i=0;i<requests.Count;i++)
        {
            VerticalVelocityRequest request=requests[i].verticalVelocity;
            if(request.Mode==VerticalVelocityMode.None)continue;

            hasVelocityRequest=true;
            if(request.Priority>highestPriority)
                highestPriority=request.Priority;
        }

        if(!hasVelocityRequest)return currentVelocity;

        float resolvedVelocity=currentVelocity;
        for(int i=0;i<requests.Count;i++)
        {
            VerticalVelocityRequest request=requests[i].verticalVelocity;
            if(request.Mode==VerticalVelocityMode.None||
               request.Priority!=highestPriority)continue;

            switch(request.Mode)
            {
                case VerticalVelocityMode.Set:
                    resolvedVelocity=request.Value;
                    break;
                case VerticalVelocityMode.AddImpulse:
                    resolvedVelocity+=request.Value;
                    break;
                case VerticalVelocityMode.Clear:
                    resolvedVelocity=0f;
                    break;
            }
        }

        return resolvedVelocity;
    }

    public MovementResult ResolveMotion(
        IReadOnlyList<MovementRequest>requests,
        float verticalVelocity,
        float deltaTime)
    {
        MovementResult result=new();
        for(int i=0;i<requests.Count;i++)
        {
            MovementRequest request=requests[i];
            result.WorldPositionDelta+=request.WorldPositionDelta;
            result.ForwardPositionDelta+=request.ForwardPositionDelta;
            result.YawDelta+=request.YawDelta;
        }

        result.WorldPositionDelta+=Vector3.up*verticalVelocity*deltaTime;
        return result;
    }
}
public class MovementResult
{
    public Vector3 WorldPositionDelta;
    public float ForwardPositionDelta;
    public float  YawDelta;
}
