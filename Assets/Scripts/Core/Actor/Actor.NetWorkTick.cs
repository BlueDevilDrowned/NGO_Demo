using UnityEngine;

public partial class Actor
{
    private void OnNetWorkTick()
    {
        //networkInput
        CaptureAndSubmitinput();
        //Simulation
        SimulateServerTick();
    }
}
