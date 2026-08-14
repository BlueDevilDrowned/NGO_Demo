using Unity.Netcode;

public partial class Actor
{
    //负责RPC这种必须要networkbehaviour的组件

    //owner发送，server接受
    [Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Owner)]
    public void SubmitPacketServerRpc(byte[] packet)
    {
       //接收后服务器审批数据，同步到权威数据板
       actorSyncSystem.ReceivePacket(packet,SycnDirection.OwnerToServer);
    }

    [Rpc(SendTo.ClientsAndHost,InvokePermission = RpcInvokePermission.Server)]
    public void SubmitPacketClientRpc(byte[] packet)
    {
       //接收后服务器审批数据，同步到权威数据板
       actorSyncSystem.ReceivePacket(packet,SycnDirection.ServerToClients);
    }
}
