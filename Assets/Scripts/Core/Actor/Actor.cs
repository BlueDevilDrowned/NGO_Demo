using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;
[RequireComponent(typeof(CharacterController))]
//
//本组件分块写
//但是Actor本体部分只管理各个组件的初始化以及spawn和Despawn，销毁等一次性调用
//Actor作为所有挂载玩家上的位移调度入口
public partial class Actor : NetworkBehaviour
{
    void Awake()
    {
        
    }
    public override void OnNetworkSpawn()
    {
        
        //加入更新调度
        NetworkManager.NetworkTickSystem.Tick+=Tick;


    }
    public override void OnNetworkDespawn()
    {
        
        //离开时注销调度
        NetworkManager.NetworkTickSystem.Tick-=Tick;
    }
    public override void OnDestroy()
    {
        
    }
}
