using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class MoveTargetBehaviour : NetworkBehaviour,IProjectileHitReceiver
{
    public Transform Obj;
    public Transform Start;
    public Transform End;
    public float MoveSpeed=1;
    private bool ToEnd;
    private readonly NetworkVariable<bool> movementPaused = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        if(NetworkManager!=null)
            NetworkManager.NetworkTickSystem.Tick+=Tick;
    }

    public override void OnNetworkDespawn()
    {
        if(NetworkManager!=null)
            NetworkManager.NetworkTickSystem.Tick-=Tick;
        base.OnNetworkDespawn();
    }

    void Tick()
    {
        if(movementPaused.Value||Obj==null||Start==null||End==null)
            return;

        Vector3 pos=Start.position;
        if(ToEnd)
        {
            pos=End.position;
        }

        Obj.position=Vector3.MoveTowards(Obj.position,pos,MoveSpeed*TickTime.deltaTime);
        if(ToEnd&&Obj.position==End.position)
        {
            
            ToEnd=false;
        }
        if(!ToEnd&&Obj.position==Start.position)
        {
            
            ToEnd=true;
        }
    }

    public void ReceiveProjectileHit(in ProjectileHitResult hit)
    {
        if(!IsServer)
            return;

        movementPaused.Value=!movementPaused.Value;
    }
}
