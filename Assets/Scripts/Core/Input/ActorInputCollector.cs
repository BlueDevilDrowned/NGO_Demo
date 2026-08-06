using System;
using UnityEngine;

public sealed class ActorInputCollector
{
    private readonly NetWorkPlayerController playerController;
    private readonly RunTimeData runTimeData;
    private readonly Transform fallbackView;

    public ActorInputCollector(
        NetWorkPlayerController playerController,
        RunTimeData runTimeData,
        Transform fallbackView)
    {
        this.playerController=playerController??
            throw new ArgumentNullException(nameof(playerController));
        this.runTimeData=runTimeData??
            throw new ArgumentNullException(nameof(runTimeData));
        this.fallbackView=fallbackView??
            throw new ArgumentNullException(nameof(fallbackView));
    }

    public void Capture(Transform view)
    {
        ActorInputData input=playerController.BuildInputData();
        Transform activeView=view!=null?view:fallbackView;
        input.ViewYaw=activeView.eulerAngles.y;

        // 输入采集独立于网络发送。Host 和将来的本地预测都可以直接读取这份命令。
        runTimeData.Input=input;
    }
}
