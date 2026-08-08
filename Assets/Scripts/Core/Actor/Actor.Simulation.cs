public partial class Actor
{
    private void SimulateServerTick()
    {
        if(!IsServer)return;

        runTimeData.locomotion=locomotionIntentProcessor.Process(
            in runTimeData.Input,
            transform.forward);

        movement.BeginTick();
        stateMachine.ServerTick();
        movement.Execute();
        aim.ServerTick();
        runTimeData.Input.Pressed=InputButtons.None;

        uint tick=(uint)NetworkManager.NetworkTickSystem.ServerTime.Tick;
        PublishServerReplication(tick);
    }
}
