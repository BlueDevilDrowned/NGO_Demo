using Unity.Netcode;
using UnityEngine;

public partial class Actor
{
    private uint lastAcceptedInputTick;
    private void CaptureAndSubmitinput()
    {
        if(!IsOwner)return;
        //读取客户端的输入
        uint tick=(uint)NetworkManager.NetworkTickSystem.LocalTime.Tick;
        ActorInputCommand command=
            netWorkPlayerController.BuildCommand(tick);
        //申请提交
        SubmitInputRpc(command);
    }
    [Rpc(SendTo.Server)]
    private void SubmitInputRpc(ActorInputCommand command)
    {
        //你是说客户端提交的需要比我服务端还快？
        if(command.Tick<=lastAcceptedInputTick)return;

        //
        command.InputMove=
            Vector2.ClampMagnitude(command.InputMove,1f);

        lastAcceptedInputTick=command.Tick;
        runTimeData.Input=command;
    }
}
