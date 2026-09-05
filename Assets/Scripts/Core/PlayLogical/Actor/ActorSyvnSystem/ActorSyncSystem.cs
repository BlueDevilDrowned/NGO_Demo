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
    public ActorSyncSystem(Actor actor)
    {
        this.actor=actor;
        OwnerToServer=new();
        ServerToClients=new();
        actor.RegisterSystem(this);
    }
    //同步功能

    private Dictionary<ushort,IActorSycnChannel>OwnerToServer;
    private Dictionary<ushort,IActorSycnChannel>ServerToClients;
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
                return;
            case SycnDirection.ServerToClients:
                ServerToClients.Add(ChannelID,channel);
                return;
        }
    }
    public void UnRegister(ushort ChannelID,SycnDirection direction)
    {
        switch(direction)
        {
            case SycnDirection.OwnerToServer:
                OwnerToServer.Remove(ChannelID);
                return;
            case SycnDirection.ServerToClients:
                ServerToClients.Remove(ChannelID);
                return;
        }
    }
    #endregion
    #region 同步更新
    
    //系统保存默认数据设置
    private int InitialReplicationBufferSize=256;
    private int MaxReplicationBufferSize=4096;
    public void Tick(uint localTick,uint serverTick)
    {
        OwnerToServerTick(localTick);
        ServerToClientsTick(serverTick);
    }
    private byte[] WritePacket(uint tick,Dictionary<ushort,IActorSycnChannel>Channels)
    {
        using FastBufferWriter writer=new(InitialReplicationBufferSize,Allocator.Temp,MaxReplicationBufferSize);
        writer.WriteValueSafe(tick);//写入tick

        int CountPosition=writer.Position;//给channel数量记录位置
        writer.WriteValueSafe((uint)0);
        uint Count=0;//等所有包写入完毕后把Count写入此位置

        foreach(var channel in Channels)
        {
            //每个channel在内部处理写入
            int PayloadPosition=writer.Position;
            //公共部分还是此系统写入

            int recordStart=writer.Position;//用于失败回滚
            
            
            ushort ChannelId=channel.Key;//写入id
            writer.WriteValueSafe(ChannelId);
            //预先写入长度
            int lengthPosition=writer.Position;
            writer.WriteValueSafe(0);
            
            int payloadStart=writer.Position;
            if(channel.Value.TryWrite(tick,writer))
            {
                Count++;//写入成功后是channel++
                //id不用重新赋值了

                int payloadEnd=writer.Position;
                writer.Seek(lengthPosition);
                int length=payloadEnd-payloadStart;
                writer.WriteValueSafe(length);
                //返回末尾
                writer.Seek(payloadEnd);

            }
            else
            {
                writer.Truncate(recordStart);
            }
        }

        int packetEndPosition=writer.Position;
        writer.Seek(CountPosition);
        writer.WriteValueSafe(Count);

        //返回到末尾
        writer.Seek(packetEndPosition);
        byte[] packet=writer.ToArray();
        //
        return packet;
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
        byte[] packet= WritePacket(tick,OwnerToServer);
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
        byte[]packet=WritePacket(tick,ServerToClients);
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
    }

}
