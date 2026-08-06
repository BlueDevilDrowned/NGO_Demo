// readonly struct 表示这是一个不可变的值对象：创建后只能读取，
// Channel 不能意外修改本次同步所处的网络身份和 Tick。
public readonly struct ActorReplicationContext
{
    public bool IsServer { get; }
    public bool IsClient { get; }
    public bool IsOwner { get; }
    public ulong OwnerClientId { get; }
    public uint Tick { get; }

    public ActorReplicationContext(
        bool isServer,
        bool isClient,
        bool isOwner,
        ulong ownerClientId,
        uint tick)
    {
        IsServer=isServer;
        IsClient=isClient;
        IsOwner=isOwner;
        OwnerClientId=ownerClientId;
        Tick=tick;
    }
}

public enum ActorReplicationDirection
{
    // 玩家自己的客户端提交给服务器，例如输入。
    OwnerToServer,
    // 服务器广播给非服务器客户端，例如权威状态。
    ServerToClients,
}
