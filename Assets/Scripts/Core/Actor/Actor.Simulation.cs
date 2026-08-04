using UnityEngine;

public partial class Actor
{
    private void SimulateServerTick()
    {
        if(!IsServer)return;
        
        //状态机更新
        //注意整合了motion，如有需要可加返回值
        stateMachine.ServerTick();
        PublishCurrentSnapshot();
    }
}
