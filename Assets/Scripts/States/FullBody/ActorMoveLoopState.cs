using UnityEngine;

public class ActorMoveLoopState : ActorBaseState
{
    private LocomotionStateType presentedState;

    public ActorMoveLoopState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        presentedState=ResolveMoveState(
            actor.simulation.locomotionData.stateType);
        actor.simulation.stateData.LastMoveState=presentedState;
        PlayLoop(presentedState,false);
        if(actor.simulation.locomotionData.stateType==LocomotionStateType.Walk)actor.audioSystem.PlayLoop("Walk");
        else if(actor.simulation.locomotionData.stateType==LocomotionStateType.Jog)actor.audioSystem.PlayLoop("Jog");
    }

    public override void Exit()
    {
        actor.audioSystem.StopLoop();
    }

    public override void ServerTick()
    {
        if(actor.simulation.CanAim)
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
        Vector3 localDirection=actor.player.InverseTransformDirection(
            actor.simulation.locomotionData.DesiredWorldMoveDirection);
        Vector2 targetParameter=new(localDirection.x,localDirection.z);

        actor.simulation.stateData.Parameter=Vector2.MoveTowards(
            actor.simulation.stateData.Parameter,
            targetParameter,
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
        DirectionalLocomotionAnimations transitions=
            GetLocomotionAnimations(state);
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
        if(transitions?.Loop!=null)
            animation.PlayTransition(transitions.Loop,options);
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
