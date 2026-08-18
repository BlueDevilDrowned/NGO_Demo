using UnityEngine;

public sealed class ActorDeathState : ActorBaseState
{
    public override bool CanEnterFrom(BaseState currentState)
    {
        if(actor.runTimeData.currentHealth<=0)return true;
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
        
        WorldWeaponPickup.Spawn(actor.weapon.CurrentWeaponId,actor.player.position,actor.player.rotation,Vector3.zero);
        if(actor.weaponEquipment.CurrentWeaponId!=0)
        {
            actor.weaponEquipment.Unequip();
        }
    }
    public override void ServerTick()
    {
        if(actor.runTimeData.Input.WasPressed(InputButtons.InputNext))
        {
            actor.health.TryRestoreFullHealth();

            //
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
        }
    }
    public override void Exit()
    {
        actor.hitboxManager.SetRagdoll(false);
        
    }
}
