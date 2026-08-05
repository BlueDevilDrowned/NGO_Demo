using System.Collections.Generic;
using UnityEngine;

public class MovementResolver
{
    public MovementResult Resolve(List<MovementRequest>requests)
    {
        MovementResult result=new();
        foreach(var request in requests)
        {
            result.WorldPositionDelta+=request.WorldPositionDelta;
            result.ForwardPositionDelta+=request.ForwardPositionDelta;
            result.YawDelta+=request.YawDelta;
        }
        requests.Clear();
        return result;
    }
}
public class MovementResult
{
    public Vector3 WorldPositionDelta;
    public float ForwardPositionDelta;
    public float  YawDelta;
}
