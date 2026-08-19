using UnityEngine;

public sealed class LocomotionIntentProcessor
{
    //计算locomotionData的数据
    public LocomotionData Process(
        in ActorInputData input,
        float viewYaw,
        Vector3 actorForward)
    {
        Vector3 worldDirection=BDMath.CalculateCameraRelativeMoveDirection(
                input.InputMove,
                viewYaw);
        //计算当前statetype
        //根据move和按键
        LocomotionStateType state=LocomotionStateType.Idle;
        if(input.InputMove.magnitude>0.01f)
        {
            state=LocomotionStateType.Walk;
            if(input.IsHeld(InputButtons.InputSprint))
            {
                state=LocomotionStateType.Jog;
            }
        }


        return new LocomotionData
        {
            DesiredWorldMoveDirection=worldDirection,
            DesiredLocalMoveAngle=BDMath.CalculateSignedPlanarAngle(
                actorForward,
                worldDirection,
                Vector3.up),
            stateType=state,
        };
    }
}
