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
                state=IsSprintSector(input.InputMove)
                    ?LocomotionStateType.Sprint
                    :LocomotionStateType.Run;
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

    private static bool IsSprintSector(Vector2 input)
    {
        if(input.sqrMagnitude<=0.0001f)return false;

        // 与八向 Mixer 的 F、LF、RF 三个扇区保持同一边界。
        float angle=Mathf.Abs(
            Mathf.Atan2(input.x,input.y)*Mathf.Rad2Deg);
        return angle<67.5f;
    }
}
