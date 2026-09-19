using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


///设计思路
///数据的同步我设计了两块板子：权威数据板，只能由服务器修改；意图输入板，客户端写入意图用的
/// 游戏核心逻辑都是由服务器计算，所以服务器读写权威数据版
/// 而客户端只负责表现，对于权威板只读。客户端只能把数据写入意图板，再传给服务器，服务器同步数据，写入权威板，再同步给各个服务器
/// 
/// 模块细化思考：
/// 数据怎么流通？
///组包解包
public class ActorSyncSystem : IActorSystem
{
    private Actor actor;
    /// <summary>客户端共享的网络 Tick 时钟。</summary>
    public NetworkTickClock Clock{get;}
    /// <summary>该角色的连续同步历史仓库。</summary>
    public ActorSyncHistory History{get;}
    public ActorSyncSystem(Actor actor)
    {
        this.actor=actor;
        Clock=ActorSyncRuntime.GetOrCreate(ActorSyncSettings.Current);
        History=new ActorSyncHistory(ActorSyncSettings.Current);
        OwnerToServer=new();
        ServerToClients=new();
        actor.RegisterSystem(this);
    }
    //同步功能

    private Dictionary<ushort,IActorSycnChannel>OwnerToServer;
    private Dictionary<ushort,IActorSycnChannel>ServerToClients;
    private readonly HashSet<IActorSycnChannel> tickOwnerToServer=new();
    private readonly HashSet<IActorSycnChannel> tickServerToClients=new();
    private readonly HashSet<IActorSycnChannel> pendingOwnerToServer=new();
    private readonly HashSet<IActorSycnChannel> pendingServerToClients=new();
    #region 注册
    public void Register(ushort ChannelID,SycnDirection direction,IActorSycnChannel channel)
    {
        if(channel==null)
            throw new ArgumentNullException(nameof(channel));

        Dictionary<ushort,IActorSycnChannel> channels=direction==SycnDirection.OwnerToServer
            ?OwnerToServer
            :ServerToClients;
        if(channels.TryGetValue(ChannelID,out IActorSycnChannel existing))
        {
            StringBuilder registered=new();
            foreach(KeyValuePair<ushort,IActorSycnChannel> entry in channels)
            {
                if(registered.Length>0)
                    registered.Append(", ");
                registered.Append(entry.Key)
                    .Append("=")
                    .Append(entry.Value?.GetType().FullName??"<null>");
            }

            throw new InvalidOperationException(
                $"Duplicate sync channel id {ChannelID} for {direction} " +
                $"on actor {actor.name} (EntityId={actor.GetEntityId()}). " +
                $"Existing={existing?.GetType().FullName??"<null>"}; " +
                $"New={channel.GetType().FullName}; " +
                $"Registered=[{registered}]");
        }

        switch(direction)
        {
            case SycnDirection.OwnerToServer:
                OwnerToServer.Add(ChannelID,channel);
                if(channel.Schedule==SyncSchedule.EveryTick)
                    tickOwnerToServer.Add(channel);
                return;
            case SycnDirection.ServerToClients:
                ServerToClients.Add(ChannelID,channel);
                if(channel.Schedule==SyncSchedule.EveryTick)
                    tickServerToClients.Add(channel);
                return;
        }
    }

    public void MarkDirty(IActorSycnChannel channel)
    {
        if(channel==null||channel.Schedule==SyncSchedule.EveryTick)return;

        HashSet<IActorSycnChannel> pending=channel.direction==SycnDirection.OwnerToServer
            ?pendingOwnerToServer
            :pendingServerToClients;
        pending.Add(channel);
    }
    public void UnRegister(ushort ChannelID,SycnDirection direction)
    {
        switch(direction)
        {
            case SycnDirection.OwnerToServer:
                if(OwnerToServer.TryGetValue(ChannelID,out IActorSycnChannel ownerChannel))
                {
                    tickOwnerToServer.Remove(ownerChannel);
                    pendingOwnerToServer.Remove(ownerChannel);
                    History.Unregister(ownerChannel);
                }
                OwnerToServer.Remove(ChannelID);
                return;
            case SycnDirection.ServerToClients:
                if(ServerToClients.TryGetValue(ChannelID,out IActorSycnChannel serverChannel))
                {
                    tickServerToClients.Remove(serverChannel);
                    pendingServerToClients.Remove(serverChannel);
                    History.Unregister(serverChannel);
                }
                ServerToClients.Remove(ChannelID);
                return;
        }
    }
    #endregion
    #region 同步更新
    
    //系统保存默认数据设置
    private int InitialReplicationBufferSize=256;
    private int MaxReplicationBufferSize=4096;
    /// <summary>
    /// 推进同步系统：localTick 用于客户端时间轴和输入发送，serverTick 用于服务器广播权威数据。
    /// </summary>
    /// <param name="localTick">当前实例所在端的本地 Tick。</param>
    /// <param name="serverTick">服务器端当前 Tick；客户端通常传入自身可用的服务器 Tick。</param>
    public void Tick(uint localTick,uint serverTick)
    {
        Clock.Advance(localTick);
        OwnerToServerTick(localTick);
        ServerToClientsTick(serverTick);
    }
    private byte[] WritePacket(
        uint tick,
        HashSet<IActorSycnChannel> tickChannels,
        HashSet<IActorSycnChannel> pendingChannels)
    {
        using FastBufferWriter writer=new(InitialReplicationBufferSize,Allocator.Temp,MaxReplicationBufferSize);
        writer.WriteValueSafe(tick);//写入tick

        int CountPosition=writer.Position;//给channel数量记录位置
        writer.WriteValueSafe((uint)0);
        uint Count=0;//等所有包写入完毕后把Count写入此位置

        foreach(IActorSycnChannel channel in tickChannels)
            TryWriteChannel(channel,tick,writer,ref Count);

        List<IActorSycnChannel> completedPending=new();
        foreach(IActorSycnChannel channel in pendingChannels)
        {
            if(TryWriteChannel(channel,tick,writer,ref Count)&&
               !channel.HasPendingData)
                completedPending.Add(channel);
        }

        foreach(IActorSycnChannel channel in completedPending)
            pendingChannels.Remove(channel);

        int packetEndPosition=writer.Position;
        writer.Seek(CountPosition);
        writer.WriteValueSafe(Count);

        //返回到末尾
        writer.Seek(packetEndPosition);
        return Count>0?writer.ToArray():null;
    }

    private static bool TryWriteChannel(
        IActorSycnChannel channel,
        uint tick,
        FastBufferWriter writer,
        ref uint count)
    {
        int recordStart=writer.Position;
        writer.WriteValueSafe(channel.ChannelId);
        int lengthPosition=writer.Position;
        writer.WriteValueSafe(0);

        int payloadStart=writer.Position;
        if(!channel.TryWrite(tick,writer))
        {
            writer.Truncate(recordStart);
            return false;
        }

        count++;
        int payloadEnd=writer.Position;
        writer.Seek(lengthPosition);
        writer.WriteValueSafe(payloadEnd-payloadStart);
        writer.Seek(payloadEnd);
        return true;
    }
    public void ReceivePacket(byte[]packet,SycnDirection direction)
    {
        Dictionary<ushort,IActorSycnChannel>Channels;
        switch(direction)
        {
            case SycnDirection.OwnerToServer:
                Channels=OwnerToServer;
                break;
            case SycnDirection.ServerToClients:
                Channels=ServerToClients;
                break;
            default:
                return;
        }
        //
        using FastBufferReader reader=new(packet,Allocator.Temp);

        try
        {
            reader.ReadValueSafe(out uint tick);
            if(direction==SycnDirection.ServerToClients)
                Clock.ObserveServerPacket(tick,actor.localTick);
            reader.ReadValueSafe(out uint ChannelCount);
            for(int i=0;i<ChannelCount;i++)
            {
                reader.ReadValueSafe(out ushort channelId);
                reader.ReadValueSafe(out int PayloadLength);

                if(PayloadLength<0||PayloadLength>reader.Length-reader.Position)
                {
                    return;//长度不合法
                }
                

                int payloadEnd=reader.Position+PayloadLength;
                if(Channels.TryGetValue(channelId,out var channel))
                {
                    channel.TryApply(tick,reader,payloadEnd);
                }

                //不管是否成功都来到下一个位置
                reader.Seek(payloadEnd);
            }
        }
        catch(OverflowException exception)
        {
            UnityEngine.Debug.LogWarning($"Actor sync packet is incomplete:{exception}");
        }
        

    }
    #region OwnerToServer
    private void OwnerToServerTick(uint tick)
    {
        if(!actor.IsOwner)return;
        byte[] packet=WritePacket(tick,tickOwnerToServer,pendingOwnerToServer);
        if(packet==null)return;
        //发送到服务器执行
        SubmitPacketServerRpc(packet);
    }
    
    private void SubmitPacketServerRpc(byte[] packet)
    {
        actor.SubmitPacketServerRpc(packet);
    }
    #endregion 
    #region ServerToClients
    
    private void ServerToClientsTick(uint tick)
    {
        if(!actor.IsServer)return;
        byte[]packet=WritePacket(tick,tickServerToClients,pendingServerToClients);
        if(packet==null)return;
        SubmitPacketClientsRpc(packet);//发送给客户端
    }
    private void SubmitPacketClientsRpc(byte[] packet)
    {
        actor.SubmitPacketClientRpc(packet);
    }
    #endregion
    
    #endregion

    public void Dispose()
    {
        OwnerToServer.Clear();
        ServerToClients.Clear();
        tickOwnerToServer.Clear();
        tickServerToClients.Clear();
        pendingOwnerToServer.Clear();
        pendingServerToClients.Clear();
        History.Dispose();
    }

    /// <summary>
    /// 按全局网络时钟计算的表现 Tick，从该 Actor 的历史仓库采样数据。
    /// </summary>
    public bool TrySamplePresentation<T>(
        IActorSycnChannel channel,
        uint localTick,
        out T value)
    {
        uint presentationTick=Clock.GetDisplayedServerTick(localTick);
        return History.TrySample(channel,presentationTick,out value,out _);
    }

}
