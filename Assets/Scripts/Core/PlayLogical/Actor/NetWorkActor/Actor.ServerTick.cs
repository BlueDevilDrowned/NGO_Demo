using UnityEngine;

public partial class Actor
{
    private void SeverTick(uint Tick)
    {
        locomotionSystem.ServerTick();
        movement.BeginTick();
        perspectiveSystem.ServerTick();
        actorStateSystem.ServerTick(Tick);
        upperBodyStateSystem.ServerTick(Tick);
        aimSystem.ServerTick();
        interactSystem.ServerTick();
        weapon.ServerTick(Tick);
        movement.Execute();
        // 权威身体旋转完成后，保存派生夹角。
        UpdateCameraBodyYawDelta();
    }

    private void UpdateCameraBodyYawDelta()
    {
        simulation.CameraBodyYawDelta=Mathf.DeltaAngle(
            transform.eulerAngles.y,
            simulation.cameraData.ViewYaw);
    }
}
