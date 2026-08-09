public partial class Actor
{
    internal void SimulateServerTick()
    {
        if(!IsServer)return;

        runTimeData.locomotion=locomotionIntentProcessor.Process(
            in runTimeData.Input,
            transform.forward);

        movement.BeginTick();
        stateMachine.ServerTick();
        upperBodyStateMachine.ServerTick();
        movement.Execute();
        aim.ServerTick();
        runTimeData.Input.Pressed=InputButtons.None;
    }
}
