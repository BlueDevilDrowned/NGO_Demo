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
}
