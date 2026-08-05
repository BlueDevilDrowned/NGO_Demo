public partial class Actor
{
    private void OnNetWorkTick()
    {
        // 服务器使用 ServerTime，纯客户端使用 LocalTime；两者都取 NGO 的离散网络 Tick。
        uint tick=IsServer
            ?(uint)NetworkManager.NetworkTickSystem.ServerTime.Tick
            :(uint)NetworkManager.NetworkTickSystem.LocalTime.Tick;

        CaptureLocalInput(tick);       // 1. Owner 采集本 Tick 输入。
        SubmitOwnerReplication(tick);  // 2. 纯客户端把注册的上行 Channel 统一提交。
        SimulateServerTick();          // 3. 只有服务器执行模拟并发布下行 Channel。
    }
}
