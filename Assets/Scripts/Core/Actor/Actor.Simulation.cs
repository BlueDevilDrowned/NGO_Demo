using UnityEngine;

public partial class Actor
{
    private void SimulateServerTick()
    {
        if(!IsServer)return;

        RefreshMovementIntent();

        //状态机更新
        //注意整合了motion，如有需要可加返回值
        stateMachine.ServerTick();
        movement.Execute();

        uint tick=(uint)NetworkManager.NetworkTickSystem.ServerTime.Tick;
        PublishServerReplication(tick);
    }

    private void RefreshMovementIntent()
    {
        Vector3 worldDirection=
            BDMath.CalculateCameraRelativeMoveDirection(
                runTimeData.Input.InputMove,
                runTimeData.Input.ViewYaw);

        runTimeData.DesiredWorldMoveDirection=worldDirection;
        runTimeData.DesiredLocalMoveAngle=
            BDMath.CalculateSignedPlanarAngle(
                transform.forward,
                worldDirection,
                Vector3.up);
    }
}
