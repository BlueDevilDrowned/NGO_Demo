public partial class Actor
{
    internal void PrepareNetworkTick(uint currentTick)
    {
        if(IsOwner)
        {
            inputCollector.Capture(Cam);
            aim.CaptureOwnerInput(ref runTimeData.Input);
        }

        SubmitOwnerReplication(currentTick);
        inputSynchronizer.ApplyPendingCommand();

        //interact
        interact.Tick();
    }
}
