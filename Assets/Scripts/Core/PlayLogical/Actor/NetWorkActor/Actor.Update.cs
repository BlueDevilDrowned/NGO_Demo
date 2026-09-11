using Unity.Netcode;
using UnityEngine;

public partial class Actor
{
    public uint serverTick{get;private set;}
    public uint localTick{get;private set;}
    //负责Tick更新，更细化的先后规则由对应函数规定
    private void Tick()
    {
        serverTick=GetServerTick();
        localTick=GetLocalTick();
        //owner同时也可以是sever
        if(IsOwner)OwnerTick(localTick);
        if(IsServer)SeverTick(serverTick);

        //数据同步系统由系统决定owner等身份逻辑
        actorSyncSystem.Tick(localTick,serverTick);
        

    }
    private void Update()
    {
        if(!IsSpawned||!systemsInitialized)return;
        if(IsOwner)
        {
            if(Input.GetKeyDown(KeyCode.Tab)) ToggleInventoryWindow();
            if(Input.GetKeyDown(KeyCode.Escape)) CloseInventoryWindow();
        }
        float deltaTime=Time.deltaTime;
        locomotionSystem.PresentationUpdate();
        healthSystem.PresentationUpdate();
        PresentationUpdate(deltaTime);
        perspectiveSystem.PresentationUpdate();
        cameraSystem.PresentationUpdate(deltaTime);//相机表现层
        aimSystem.PresentationUpdate();
        interactSystem.PresentationUpdate();
        weaponInventory.PresentationUpdate();
        inventorySystem.PresentationUpdate();
        weapon.PresentationUpdate();
    }

    private InventoryWindowUI inventoryWindow;
    private void ToggleInventoryWindow()
    {
        if(inventoryWindow!=null) { CloseInventoryWindow(); return; }
        if(inventorySystem?.ActiveBackpack!=null)
            inventoryWindow=InventoryWindowUI.Open(inventorySystem);
    }
    private void CloseInventoryWindow()
    {
        if(inventoryWindow==null) return;
        inventoryWindow.Close();
        inventoryWindow=null;
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
