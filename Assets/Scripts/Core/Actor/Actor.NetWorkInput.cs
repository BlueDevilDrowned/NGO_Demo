public partial class Actor
{
    private void CaptureLocalInput(uint tick)
    {
        // 每个客户端只读取自己拥有的角色输入，其他角色等待服务器下行快照。
        if(!IsOwner)return;

        ActorInputCommand command=
            netWorkPlayerController.BuildCommand(tick);
        command.ViewYaw=
            Cam!=null?Cam.eulerAngles.y:transform.eulerAngles.y;
        // Channel 从 RunTimeData 取快照，因此采集必须发生在统一组包之前。
        runTimeData.Input=command;
    }
}
