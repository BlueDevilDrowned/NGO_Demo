using Unity.Collections;
using Unity.Netcode;

public partial class Actor
{
    // Writer 使用非托管内存。初始容量不足时可增长，但绝不超过最大容量。
    private const int InitialReplicationBufferSize=256;
    private const int MaxReplicationBufferSize=4096;

    private ActorSnapshotReplicator snapshotReplicator;
    private ActorInputReplicationChannel inputReplicationChannel;
    private ActorStateReplicationChannel stateReplicationChannel;

    private void InitializeReplication()
    {
        // Actor 是组合入口：具体系统创建完后，在这里建立 Channel 关联并统一注册。
        snapshotReplicator=new ActorSnapshotReplicator();
        //输入同步
        inputReplicationChannel=
            new ActorInputReplicationChannel(runTimeData);
        //状态机同步
        stateReplicationChannel=
            new ActorStateReplicationChannel(
                runTimeData,
                stateMachine,
                StateRegistry,
                RefreshMovementIntent,
                ApplyNetworkState);

        snapshotReplicator.Register(inputReplicationChannel);
        snapshotReplicator.Register(stateReplicationChannel);
    }

    private ActorReplicationContext CreateReplicationContext(uint tick)
    {
        // 把 NetworkBehaviour 身份集中转换成只读上下文，Channel 不需要依赖 Actor。
        return new ActorReplicationContext(
            IsServer,
            IsClient,
            IsOwner,
            OwnerClientId,
            tick);
    }

    private void SubmitOwnerReplication(uint tick)
    {
        // Host 已经直接持有权威运行时数据，不需要把输入 RPC 给自己。
        if(!IsOwner||IsServer)return;

        ActorReplicationContext context=CreateReplicationContext(tick);
        // using 保证方法 return 或发生异常时都会 Dispose，释放 Writer 的非托管内存。
        using FastBufferWriter writer=new(
            InitialReplicationBufferSize,
            Allocator.Temp,
            MaxReplicationBufferSize);

        ushort channelCount=snapshotReplicator.WriteAll(
            ActorReplicationDirection.OwnerToServer,
            in context,
            writer);
        if(channelCount==0)return;

        // Writer 只负责组包；真正的网络发送发生在下面的 RPC。
        // ToArray 会生成托管 byte[]，目前清晰易用，但每 Tick 会产生一次 GC 分配。
        SubmitReplicationRpc(writer.ToArray());
    }

    // SendTo.Server：包只发往服务器。
    // InvokePermission.Owner：只有该 NetworkObject 的 Owner 能调用，阻止其他客户端伪造输入。
    // Unreliable：输入每 Tick 都会刷新，旧包丢失时不重传，避免可靠消息排队增加延迟。
    [Rpc(
        SendTo.Server,
        InvokePermission=RpcInvokePermission.Owner,
        Delivery=RpcDelivery.Unreliable)]
    private void SubmitReplicationRpc(byte[] packet)
    {
        if(packet==null||
           packet.Length==0||
           packet.Length>MaxReplicationBufferSize)return;

        uint tick=(uint)NetworkManager.NetworkTickSystem.ServerTime.Tick;
        ActorReplicationContext context=CreateReplicationContext(tick);
        // FastBufferReader 与 Writer 相反：它按相同协议从 byte[] 依次还原字段。
        using FastBufferReader reader=new(packet,Allocator.Temp);

        snapshotReplicator.ReadAllAndApply(
            ActorReplicationDirection.OwnerToServer,
            in context,
            reader);
    }

    private void PublishServerReplication(uint tick)
    {
        if(!IsServer)return;

        ActorReplicationContext context=CreateReplicationContext(tick);
        // 服务器把所有 ServerToClients Channel 集中写入同一个下行包。
        using FastBufferWriter writer=new(
            InitialReplicationBufferSize,
            Allocator.Temp,
            MaxReplicationBufferSize);

        ushort channelCount=snapshotReplicator.WriteAll(
            ActorReplicationDirection.ServerToClients,
            in context,
            writer);
        if(channelCount==0)return;

        ApplyReplicationRpc(writer.ToArray());
    }

    // NotServer 会发给所有非服务器客户端；Host 不会收到，避免重复 Apply 本地权威状态。
    // Server 权限保证客户端不能调用这个下行 RPC。
    [Rpc(
        SendTo.NotServer,
        InvokePermission=RpcInvokePermission.Server,
        Delivery=RpcDelivery.Unreliable)]
    private void ApplyReplicationRpc(byte[] packet)
    {
        if(packet==null||
           packet.Length==0||
           packet.Length>MaxReplicationBufferSize)return;

        uint tick=(uint)NetworkManager.NetworkTickSystem.LocalTime.Tick;
        ActorReplicationContext context=CreateReplicationContext(tick);
        using FastBufferReader reader=new(packet,Allocator.Temp);

        snapshotReplicator.ReadAllAndApply(
            ActorReplicationDirection.ServerToClients,
            in context,
            reader);
    }

    private void ApplyNetworkState(ActorStateType stateType)
    {
        ActorBaseState targetState=StateRegistry.GetState(stateType);
        if(targetState==null)return;
        // 同一状态仍需更新参数，但不应重复 Exit/Enter。
        if(ReferenceEquals(stateMachine.CurrentState,targetState))return;

        stateMachine.ChangeState(targetState);
    }
}
