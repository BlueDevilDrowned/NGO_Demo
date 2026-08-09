using System;
using Unity.Netcode;
using UnityEngine;

public static class TickTime
{
    public static float deltaTime
    {
        get
        {
            NetworkManager manager=NetworkManager.Singleton;
            if(manager==null||!manager.IsListening)
            {
                throw new InvalidOperationException("Network simulation has not started.");
            }
            return manager.NetworkTickSystem.LocalTime.FixedDeltaTime;
        }
    }

    public static uint CurrentLocalTick
    {
        get
        {
            NetworkManager manager=GetListeningManager();
            return (uint)manager.NetworkTickSystem.LocalTime.Tick;
        }
    }

    public static uint CurrentServerTick
    {
        get
        {
            NetworkManager manager=GetListeningManager();
            if(!manager.IsServer)
                throw new InvalidOperationException(
                    "Authoritative server tick is only available on the server.");

            return (uint)manager.NetworkTickSystem.ServerTime.Tick;
        }
    }

    public static uint TickRate
    {
        get
        {
            NetworkManager manager=NetworkManager.Singleton;
            if(manager==null)
                throw new InvalidOperationException(
                    "NetworkManager is unavailable.");

            return manager.NetworkConfig.TickRate;
        }
    }

    private static NetworkManager GetListeningManager()
    {
        NetworkManager manager=NetworkManager.Singleton;
        if(manager==null||!manager.IsListening)
            throw new InvalidOperationException(
                "Network simulation has not started.");

        return manager;
    }
}
