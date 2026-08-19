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
        StartFootIsL=actor.simulation.stateData.StartFootIsL;
        presentedState=ResolveMoveState(
            actor.simulation.locomotionData.stateType);
        actor.simulation.stateData.LastMoveState=presentedState;
        PlayLoop(presentedState,false);
        if(actor.simulation.locomotionData.stateType==LocomotionStateType.Walk)actor.audioSystem.PlayLoop("Walk");
        else if(actor.simulation.locomotionData.stateType==LocomotionStateType.Jog)actor.audioSystem.PlayLoop("Jog");
    }

    public override void Exit()
    {
        IsLeaning=false;
        actor.audioSystem.StopLoop();
    }

    public override void ServerTick()
    {
        if(actor.simulation.WantAim)
        {
            //切换瞄准idle
            stateMachine.ChangeState(stateRegistry.GetState<ActorAimMoveState>());
            return;
        }
        if(!actor.simulation.WantMove)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorMoveStopState>());
            return;
        }

        LocomotionStateType moveState=ResolveMoveState(
            actor.simulation.locomotionData.stateType);
        actor.simulation.stateData.LastMoveState=moveState;
        //根据状态决定参数
        float maxYawDelta=GetMaxRotation(moveState)*TickTime.deltaTime;
        float yawDelta=Mathf.Clamp(
            actor.simulation.locomotionData.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        float targetParameter=maxYawDelta>Mathf.Epsilon
            ?yawDelta/maxYawDelta
            :0f;

        actor.simulation.stateData.Parameter=Vector2.MoveTowards(
            actor.simulation.stateData.Parameter,
            new Vector2(targetParameter,0f),
            actor.actorSO.animationSO.Walk_Loop_SmoothFactor*TickTime.deltaTime);
    }

    public override void PresentationUpdate(float deltaTime)
    {
        //locomotion状态改变，需要切换动画，这部分作用在表现层，所以需要所有客户端自己更新
        LocomotionStateType targetState=
            actor.simulation.locomotionData.stateType;
        if(targetState!=LocomotionStateType.Idle&&targetState!=presentedState)
        {
            PlayLoop(ResolveMoveState(targetState),true);
            return;
        }

        if(IsLeaning||Mathf.Abs(actor.simulation.stateData.Parameter.x)<=LeanEnterThreshold)
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
            ?actor.actorSO.animancerData.Jog
            :actor.actorSO.animancerData.Walk;
        animation.PlayTransition(transitions.Loop_Lean,options);
        //同时换音效
        if(presentedState==LocomotionStateType.Jog&&!actor.audioSystem.IsLoopPlaying("Jog"))
            actor.audioSystem.PlayLoop("Jog");
        else if(presentedState==LocomotionStateType.Walk&&!actor.audioSystem.IsLoopPlaying("Walk"))
            actor.audioSystem.PlayLoop("Walk");
    }

    public override void ApplyParameter()
    {
        animation.SetMixerParameter(actor.simulation.stateData.Parameter);
    }

    public override void EvaluateMotion()
    {
        LocomotionStateType moveState=ResolveMoveState(
            actor.simulation.locomotionData.stateType);
        float maxYawDelta=GetMaxRotation(moveState)*TickTime.deltaTime;
        float yawDelta=Mathf.Clamp(
            actor.simulation.locomotionData.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        float inputAmount=Mathf.Clamp01(actor.simulation.inputData.InputMove.magnitude);
        float speed=moveState==LocomotionStateType.Jog
            ?actor.actorSO.controllerSO.JogSpeed
            :actor.actorSO.controllerSO.WalkSpeed;

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
            ?actor.actorSO.animancerData.Jog
            :actor.actorSO.animancerData.Walk;
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
            ?actor.actorSO.controllerSO.JogmaxRotation
            :actor.actorSO.controllerSO.WalkmaxRotation;
    }
}
