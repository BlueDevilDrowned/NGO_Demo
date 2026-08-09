using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// ActorTickScheduler 是一个静态类，用于管理和调度所有Actor的网络tick事件
/// 它负责在适当的网络tick时调用Actor的各种方法，如准备网络tick、模拟服务器tick和发布服务器复制
/// </summary>
public static class ActorTickScheduler
{
    // 存储所有已注册的Actor列表
    private static readonly List<Actor> actors=new();
    // 网络管理器引用，用于获取网络tick系统
    private static NetworkManager networkManager;
    // 标志位，表示是否已订阅网络tick事件
    private static bool isSubscribed;

    /// <summary>
    /// 注册一个Actor到调度器中
    /// </summary>
    /// <param name="actor">要注册的Actor实例</param>
    public static void Register(Actor actor)
    {
        // 如果actor为空或已存在，则直接返回
        if(actor==null||actors.Contains(actor))return;

        // 添加actor到列表
        actors.Add(actor);
        // 如果已经订阅，则直接返回
        if(isSubscribed)return;

        // 获取网络管理器单例
        networkManager=NetworkManager.Singleton;
        // 如果网络管理器不存在，记录错误并返回
        if(networkManager==null)
        {
            Debug.LogError("Actor tick scheduler could not find NetworkManager.");
            return;
        }

        // 订阅网络tick事件
        networkManager.NetworkTickSystem.Tick+=OnNetworkTick;
        isSubscribed=true;
    }

    /// <summary>
    /// 从调度器中取消注册一个Actor
    /// </summary>
    /// <param name="actor">要取消注册的Actor实例</param>
    public static void Unregister(Actor actor)
    {
        // 如果actor不为空，取消其所有投射物
        if(actor!=null)
            ProjectileSystem.Shared.CancelByOwner(actor);
        // 从列表中移除actor
        actors.Remove(actor);
        // 如果还有其他actor或未订阅，则直接返回
        if(actors.Count>0||!isSubscribed)return;

        // 取订网络tick事件
        if(networkManager!=null)
            networkManager.NetworkTickSystem.Tick-=OnNetworkTick;
        isSubscribed=false;
        networkManager=null;
        // 清除所有投射物
        ProjectileSystem.Shared.Clear();
    }

    /// <summary>
    /// 网络tick事件的处理方法
    /// </summary>
    private static void OnNetworkTick()
    {
        // 如果网络管理器不存在或未监听，则直接返回
        if(networkManager==null||!networkManager.IsListening)return;

        // 获取当前tick值，服务器使用服务器tick，客户端使用本地tick
        uint currentTick=networkManager.IsServer
            ?TickTime.CurrentServerTick
            :TickTime.CurrentLocalTick;

        // 对所有已注册的actor执行准备网络tick操作
        for(int i=0;i<actors.Count;i++)
        {
            Actor actor=actors[i];
            if(actor!=null&&actor.IsSpawned)
                actor.PrepareNetworkTick(currentTick);
        }

        // 如果不是服务器，则跳过后续操作
        if(!networkManager.IsServer)return;

        // 对所有已注册的actor执行服务器模拟tick
        for(int i=0;i<actors.Count;i++)
        {
            Actor actor=actors[i];
            if(actor!=null&&actor.IsSpawned)
                actor.SimulateServerTick();
        }

        // 同步物理变换
        Physics.SyncTransforms();
        // 执行投射物系统的服务器tick
        ProjectileSystem.Shared.ServerTick(currentTick,TickTime.deltaTime);

        // 对所有已注册的actor发布服务器复制
        for(int i=0;i<actors.Count;i++)
        {
            Actor actor=actors[i];
            if(actor!=null&&actor.IsSpawned)
                actor.PublishServerReplication(currentTick);
        }
    }
}
