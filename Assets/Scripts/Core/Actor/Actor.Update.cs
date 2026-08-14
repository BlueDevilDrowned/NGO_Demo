using Unity.Netcode;
using UnityEngine;

public partial class Actor
{
    //负责Tick更新，更细化的先后规则由对应函数规定
    private void Tick()
    {
        uint serverTick=GetServerTick();
        uint localTick=GetLocalTick();
        //owner同时也可以是sever
        if(IsOwner)OwnerTick(localTick);
        if(IsServer)SeverTick(serverTick);

        //数据同步系统由系统决定owner等身份逻辑
        actorSyncSystem.Tick(localTick,serverTick);
        

    }
    private void Update()
    {
        PresentationUpdate(Time.deltaTime);
    }
    private uint GetServerTick()
    {
        return (uint)NetworkManager.NetworkTickSystem.ServerTime.Tick;
    }
    private uint GetLocalTick()
    {
        return (uint)NetworkManager.NetworkTickSystem.LocalTime.Tick;
    }
}
