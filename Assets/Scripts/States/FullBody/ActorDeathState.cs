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
