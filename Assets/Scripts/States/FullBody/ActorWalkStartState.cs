using UnityEngine;

public class ActorWalkStartState : ActorBaseState
{
    public ActorWalkStartState(Actor actor) : base(actor)
    {
    }
    bool IsL;
    Vector2 currentInput;

    //
    float MoveSpeed=1f;
    public override void Enter()
    {
        IsL=false;
        currentInput=Vector2.zero;
        //根据输入选择起步方向
        //不过是混合动画，所以直接输入输入值就行了
        animation.PlayTransition(actor.animancerData.Walk_Start,AnimPlayOptions.Default);
        animation.SetMixerParameter(actor.runTimeData.Input.InputMove);
        currentInput=actor.runTimeData.Input.InputMove;

        if(actor.runTimeData.Input.InputMove.x<=0)IsL=true;
        else IsL=false;
        actor.runTimeData.blackboard.StartFootIsL=IsL;
        stateMachine.SetOnEndCallback(OnEndCallback);
    }
    private void OnEndCallback()
    {
        if(stateMachine.CurrentState!=this)return;
        //进入loop
        stateMachine.ChangeState(stateRegistry.GetState<ActorWalkLoopState>());

    }
    public override void ServerTick()
    {
        if(!actor.runTimeData.WantMove)
        {
            //取消移动,进入idle/stop
            //播放》=0.5进入stop
            if(NormalizedTime>=0.5f)
            {
                stateMachine.ChangeState(stateRegistry.GetState<ActorWalkStopState>());
            }
            else
            {
                stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
            }
        }
    }
    //对于起步动画，平滑改变方向输入
    public override void ApplyParameter()
    {
        currentInput=Vector2.MoveTowards(currentInput,actor.runTimeData.Input.InputMove,MoveSpeed*Time.deltaTime);
        animation.SetMixerParameter(currentInput);
    }
}
