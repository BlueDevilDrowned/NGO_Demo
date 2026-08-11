using UnityEngine;

public class ActorMoveLoopState : ActorBaseState
{
    private const float LeanEnterThreshold=0.05f;

    private bool StartFootIsL;
    private bool IsLeaning;
    private LocomotionStateType presentedState;

    public ActorMoveLoopState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        IsLeaning=false;
        StartFootIsL=actor.runTimeData.blackboard.StartFootIsL;
        presentedState=ResolveMoveState(
            actor.runTimeData.locomotion.stateType);
        actor.runTimeData.blackboard.LastMoveState=presentedState;
        PlayLoop(presentedState,false);
        if(actor.runTimeData.locomotion.stateType==LocomotionStateType.Walk)actor.actorAudio.PlayLoop("Walk");
        else if(actor.runTimeData.locomotion.stateType==LocomotionStateType.Jog)actor.actorAudio.PlayLoop("Jog");
    }

    public override void Exit()
    {
        IsLeaning=false;
        actor.actorAudio.StopLoop();
    }

    public override void ServerTick()
    {
        if(actor.runTimeData.WantAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimMoveState>());
            return;
        }
        if(!actor.runTimeData.WantMove)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorMoveStopState>());
            return;
        }

        LocomotionStateType moveState=ResolveMoveState(
            actor.runTimeData.locomotion.stateType);
        actor.runTimeData.blackboard.LastMoveState=moveState;
        //根据状态决定参数
        float maxYawDelta=GetMaxRotation(moveState)*TickTime.deltaTime;
        float yawDelta=Mathf.Clamp(
            actor.runTimeData.locomotion.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        float targetParameter=maxYawDelta>Mathf.Epsilon
            ?yawDelta/maxYawDelta
            :0f;

        actor.runTimeData.blackboard.Parameter=Vector2.MoveTowards(
            actor.runTimeData.blackboard.Parameter,
            new Vector2(targetParameter,0f),
            actor.animationSO.Walk_Loop_SmoothFactor*TickTime.deltaTime);
    }

    public override void PresentationUpdate(float deltaTime)
    {
        //locomotion状态改变，需要切换动画，这部分作用在表现层，所以需要所有客户端自己更新
        LocomotionStateType targetState=
            actor.runTimeData.locomotion.stateType;
        if(targetState!=LocomotionStateType.Idle&&targetState!=presentedState)
        {
            PlayLoop(ResolveMoveState(targetState),true);
            return;
        }

        if(IsLeaning||Mathf.Abs(actor.runTimeData.blackboard.Parameter.x)<=LeanEnterThreshold)
            return;

        IsLeaning=true;

        float normalizedTime=NormalizedTime;
        if(StartFootIsL)
            normalizedTime+=0.5f;

        AnimPlayOptions options=new()
        {
            FadeDuration=0.1f,
            Speed=1f,
            NormalizedTime=Mathf.Repeat(normalizedTime,1f),
        };
        //判断状态切换
        LocomotionTransition transitions=presentedState==LocomotionStateType.Jog
            ?actor.animancerData.Jog
            :actor.animancerData.Walk;
        animation.PlayTransition(transitions.Loop_Lean,options);
        //同时换音效
        if(presentedState==LocomotionStateType.Jog&&!actor.actorAudio.IsLoopPlaying("Jog"))
            actor.actorAudio.PlayLoop("Jog");
        else if(presentedState==LocomotionStateType.Walk&&!actor.actorAudio.IsLoopPlaying("Walk"))
            actor.actorAudio.PlayLoop("Walk");
    }

    public override void ApplyParameter()
    {
        animation.SetMixerParameter(actor.runTimeData.blackboard.Parameter);
    }

    public override void EvaluateMotion()
    {
        LocomotionStateType moveState=ResolveMoveState(
            actor.runTimeData.locomotion.stateType);
        float maxYawDelta=GetMaxRotation(moveState)*TickTime.deltaTime;
        float yawDelta=Mathf.Clamp(
            actor.runTimeData.locomotion.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        float inputAmount=Mathf.Clamp01(actor.runTimeData.Input.InputMove.magnitude);
        float speed=moveState==LocomotionStateType.Jog
            ?actor.controllerSO.JogSpeed
            :actor.controllerSO.WalkSpeed;

        MovementRequest request=new()
        {
            Source=moveState==LocomotionStateType.Jog?"Jog":"Walk",
            WorldPositionDelta=Vector3.zero,
            ForwardPositionDelta=speed*inputAmount*TickTime.deltaTime,
            YawDelta=yawDelta,
        };

        actor.movement.Submit(request);
    }

    private void PlayLoop(
        LocomotionStateType state,
        bool preserveNormalizedTime)
    {
        float normalizedTime=Mathf.Repeat(NormalizedTime,1f);
        LocomotionTransition transitions=state==LocomotionStateType.Jog
            ?actor.animancerData.Jog
            :actor.animancerData.Walk;
        AnimPlayOptions options=AnimPlayOptions.Default;
        if(preserveNormalizedTime)
        {
            options=new AnimPlayOptions
            {
                FadeDuration=0.1f,
                Speed=1f,
                NormalizedTime=normalizedTime,
            };
        }

        presentedState=state;
        IsLeaning=false;
        animation.PlayTransition(
            !StartFootIsL?transitions.Loop_L:transitions.Loop_R,
            options);
    }

    private static LocomotionStateType ResolveMoveState(
        LocomotionStateType state)
    {
        return state==LocomotionStateType.Jog
            ?LocomotionStateType.Jog
            :LocomotionStateType.Walk;
    }

    private float GetMaxRotation(LocomotionStateType state)
    {
        return state==LocomotionStateType.Jog
            ?actor.controllerSO.JogmaxRotation
            :actor.controllerSO.WalkmaxRotation;
    }
}
