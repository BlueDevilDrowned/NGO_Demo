using UnityEngine;

public sealed class ActorDeathState : ActorBaseState
{
    public override bool CanEnterFrom(BaseState currentState)
    {
        if(actor.simulation.currentHealth<=0)return true;
        return false;
    }
    public ActorDeathState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        //禁用movement和animation
        //启用布娃娃
        actor.hitboxManager.SetRagdoll(true);

        //掉落武器
        if(!actor.IsServer)return;
        
        WeaponInventorySystem inventory=actor.weaponInventory;
        if(inventory?.Data?.weaponIds==null)return;

        for(byte slot=0;slot<inventory.Data.weaponIds.Count;slot++)
        {
            if(!inventory.CanDrop(slot))continue;
            if(!inventory.TryDropWeapon(slot,out ushort weaponId))continue;

            Vector3 inheritedVelocity=actor.movement?.Velocity??Vector3.zero;
            float throwSpeed=actor.actorSO?.controllerSO?.WeaponDropThrowSpeed??0f;
            Vector3 dropVelocity=inheritedVelocity+
                actor.transform.forward*throwSpeed;

            ControllerSO controller=actor.actorSO?.controllerSO;
            Vector3 dropPosition=controller!=null
                ?controller.GetWeaponDropPosition(actor.transform)
                :actor.transform.position;

            WorldWeaponPickup.Spawn(
                weaponId,
                dropPosition,
                actor.transform.rotation,
                dropVelocity);
        }
    }
    public override void ServerTick()
    {
        if(actor.simulation.inputData.WasPressed(InputButtons.InputNext))
        {
            actor.healthSystem.TryRestoreFullHealth();

            //
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
        }
    }
    public override void Exit()
    {
        actor.hitboxManager.SetRagdoll(false);
        
    }
}
