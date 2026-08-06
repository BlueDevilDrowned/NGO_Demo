using UnityEngine;

public sealed class LocomotionIntentProcessor
{
    //计算locomotionData的数据
    public LocomotionData Process(in ActorInputData input,Vector3 actorForward)
    {
        Vector3 worldDirection=BDMath.CalculateCameraRelativeMoveDirection(
                input.InputMove,
                input.ViewYaw);

        return new LocomotionData
        {
            DesiredWorldMoveDirection=worldDirection,
            DesiredLocalMoveAngle=BDMath.CalculateSignedPlanarAngle(
                actorForward,
                worldDirection,
                Vector3.up),
        };
    }
}
