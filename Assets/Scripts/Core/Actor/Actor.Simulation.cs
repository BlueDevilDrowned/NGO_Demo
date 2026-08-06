public partial class Actor
{
    private void SimulateServerTick()
    {
        if(!IsServer)return;

        runTimeData.locomotion=locomotionIntentProcessor.Process(
            in runTimeData.Input,
            transform.forward);

        //状态机更新
        //注意整合了motion，如有需要可加返回值
        stateMachine.ServerTick();
        movement.Execute();
        runTimeData.Input.Pressed=InputButtons.None;

        uint tick=(uint)NetworkManager.NetworkTickSystem.ServerTime.Tick;
        PublishServerReplication(tick);
    }

}
