public partial class Actor
{
    private void OnNetWorkTick()
    {
        uint tick=IsServer
            ?(uint)NetworkManager.NetworkTickSystem.ServerTime.Tick
            :(uint)NetworkManager.NetworkTickSystem.LocalTime.Tick;

        if(IsOwner)
        {
            inputCollector.Capture(Cam);
            aim.CaptureOwnerInput(ref runTimeData.Input);
        }

        SubmitOwnerReplication(tick);
        inputSynchronizer.ApplyPendingCommand();
        SimulateServerTick();
    }
}
