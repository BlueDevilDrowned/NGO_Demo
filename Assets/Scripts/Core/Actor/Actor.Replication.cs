using Unity.Collections;
using Unity.Netcode;

public partial class Actor
{
    // Writer 使用非托管内存。初始容量不足时可增长，但绝不超过最大容量。
    private const int InitialReplicationBufferSize=256;
    private const int MaxReplicationBufferSize=4096;
    //需要同步的数据Channel
    private ActorSnapshotReplicator snapshotReplicator;
    private ActorInputCommandProducer inputCommandProducer;
    private ActorInputReplicationChannel inputReplicationChannel;
    private LocomotionSnapshotProducer locomotionSnapshotProducer;
    private LocomotionReplicationChannel locomotionReplicationChannel;
    private ActorStateSnapshotProducer stateSnapshotProducer;
    private ActorStateReplicationChannel stateReplicationChannel;
    private UpperBodyStateSnapshotProducer upperBodyStateSnapshotProducer;
    private UpperBodyStateReplicationChannel upperBodyStateReplicationChannel;
    private WeaponSnapshotProducer weaponSnapshotProducer;
    private WeaponReplicationChannel weaponReplicationChannel;
    private AimSnapshotProducer aimSnapshotProducer;
    private AimReplicationChannel aimReplicationChannel;

    private void InitializeReplication()
    {
        // Actor 是组合入口；Transport 只注册 Channel，不解释各类数据的业务含义。
        snapshotReplicator=new ActorSnapshotReplicator();
        inputCommandProducer=new ActorInputCommandProducer(runTimeData);
        inputReplicationChannel=new ActorInputReplicationChannel(
            inputCommandProducer,
            inputCommandConsumer);
        locomotionSnapshotProducer=new LocomotionSnapshotProducer(runTimeData);
        locomotionReplicationChannel=new LocomotionReplicationChannel(
            locomotionSnapshotProducer,
            locomotionSnapshotConsumer);
        stateSnapshotProducer=new ActorStateSnapshotProducer(
            runTimeData,
            stateMachine,
            StateRegistry);
        stateReplicationChannel=new ActorStateReplicationChannel(
            stateSnapshotProducer,
            stateSnapshotConsumer);
        upperBodyStateSnapshotProducer=new UpperBodyStateSnapshotProducer(
            upperBodyStateMachine,
            UpperBodyStateRegistry);
        upperBodyStateReplicationChannel=new UpperBodyStateReplicationChannel(
            upperBodyStateSnapshotProducer,
            upperBodyStateSnapshotConsumer);
        weaponSnapshotProducer=new WeaponSnapshotProducer(weapon);
        weaponReplicationChannel=new WeaponReplicationChannel(
            weaponSnapshotProducer,
            weaponSnapshotConsumer);
        aimSnapshotProducer=new AimSnapshotProducer(runTimeData);
        aimReplicationChannel=new AimReplicationChannel(
            aimSnapshotProducer,
            aimSnapshotConsumer);

        snapshotReplicator.Register(inputReplicationChannel);
        snapshotReplicator.Register(locomotionReplicationChannel);
        snapshotReplicator.Register(stateReplicationChannel);
        snapshotReplicator.Register(upperBodyStateReplicationChannel);
        snapshotReplicator.Register(weaponReplicationChannel);
        snapshotReplicator.Register(aimReplicationChannel);
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
        // Host 已在本地持有权威运行数据，不需要通过 RPC 把输入发给自己。
        if(!IsOwner||IsServer)return;

        ActorReplicationContext context=CreateReplicationContext(tick);
        // using 保证提前 return 或异常时也会释放 Writer 的非托管内存。
        using FastBufferWriter writer=new(
            InitialReplicationBufferSize,
            Allocator.Temp,
            MaxReplicationBufferSize);

        ushort channelCount=snapshotReplicator.WriteAll(
            ActorReplicationDirection.OwnerToServer,
            in context,
            writer);
        if(channelCount==0)return;

        // Writer 只负责组包，RPC 才真正发送。ToArray 每 Tick 会产生一次托管分配。
        SubmitReplicationRpc(writer.ToArray());
    }

    // OwnerToServer：只有对象 Owner 能调用，只发送给服务器。
    // Unreliable：新输入会持续覆盖旧输入，丢失旧包时无需排队重传。
    [Rpc(
        SendTo.Server,
        InvokePermission=RpcInvokePermission.Owner,
        Delivery=RpcDelivery.Unreliable)]
    private void SubmitReplicationRpc(byte[] packet)
    {
        if(packet==null||
           packet.Length==0||
           packet.Length>MaxReplicationBufferSize)return;

        uint currentServerTick=TickTime.CurrentServerTick;
        ActorReplicationContext context=CreateReplicationContext(currentServerTick);
        // Reader 按 Writer 相同的字段顺序，把 byte[] 还原为各 Channel 的数据。
        using FastBufferReader reader=new(packet,Allocator.Temp);

        snapshotReplicator.ReadAllAndApply(
            ActorReplicationDirection.OwnerToServer,
            in context,
            reader);
    }

    internal void PublishServerReplication(uint tick)
    {
        if(!IsServer)return;

        ActorReplicationContext context=CreateReplicationContext(tick);
        // 所有 ServerToClients Channel 在这里写入同一个下行包。
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

    // NotServer 不会把包再发给 Host；Server 权限阻止客户端伪造下行快照。
    [Rpc(
        SendTo.NotServer,
        InvokePermission=RpcInvokePermission.Server,
        Delivery=RpcDelivery.Unreliable)]
    private void ApplyReplicationRpc(byte[] packet)
    {
        if(packet==null||
           packet.Length==0||
           packet.Length>MaxReplicationBufferSize)return;

        uint currentLocalTick=TickTime.CurrentLocalTick;
        ActorReplicationContext context=CreateReplicationContext(currentLocalTick);
        using FastBufferReader reader=new(packet,Allocator.Temp);

        snapshotReplicator.ReadAllAndApply(
            ActorReplicationDirection.ServerToClients,
            in context,
            reader);
    }
}
