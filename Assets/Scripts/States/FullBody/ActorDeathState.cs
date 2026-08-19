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
        
        ushort weaponId=actor.weaponEquipment.CurrentWeaponId;
        if(weaponId==0)return;

        WorldWeaponPickup.Spawn(
            weaponId,
            actor.player.position,
            actor.player.rotation,
            Vector3.zero);
        actor.weaponEquipment.Unequip();
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
