using UnityEngine;

public class ActorWalkStopState : ActorBaseState
{
    public override ActorStateType StateType=>ActorStateType.WalkStop;
    public ActorWalkStopState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        //先不管左右脚
        animation.PlayTransition(actor.animancerData.Walk_Stop_L,AnimPlayOptions.Default);
        stateMachine.SetOnEndCallback(OnEndCallback);
    }
    private void OnEndCallback()
    {
        if(stateMachine.CurrentState!=this)return;
        //进入loop
        stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());

    }
}
