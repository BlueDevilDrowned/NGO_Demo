using UnityEngine;

public class ActorMoveLoopState : ActorBaseState
{
    private LocomotionStateType presentedState;
    private Vector2 presentedParameter;

    public ActorMoveLoopState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        presentedState=ResolveMoveState(
            actor.simulation.locomotionData.stateType);
        SetMoveParameter(true,0f);
        PlayLoop(presentedState,false);
        RefreshMoveAudio();
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
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
            return;
        }

    }

    public override void PresentationUpdate(float deltaTime)
    {
        SetMoveParameter(false,deltaTime);

        //locomotion状态改变，需要切换动画，这部分作用在表现层，所以需要所有客户端自己更新
        LocomotionStateType locomotionState=
            actor.simulation.locomotionData.stateType;
        LocomotionStateType targetState=ResolveMoveState(locomotionState);
        if(locomotionState!=LocomotionStateType.Idle&&
           targetState!=presentedState)
        {
            PlayLoop(targetState,true);
            RefreshMoveAudio();
            return;
        }

        if(presentedState==LocomotionStateType.Run&&!actor.audioSystem.IsLoopPlaying("Jog"))
            actor.audioSystem.PlayLoop("Jog");
        else if(presentedState==LocomotionStateType.Walk&&!actor.audioSystem.IsLoopPlaying("Walk"))
            actor.audioSystem.PlayLoop("Walk");
    }

    public override void ApplyParameter()
    {
        animation.SetMixerParameter(presentedParameter);
    }

    public override void EvaluateMotion()
    {
        LocomotionStateType moveState=
            actor.simulation.locomotionData.stateType;
        float inputAmount=Mathf.Clamp01(actor.simulation.inputData.InputMove.magnitude);
        float speed=actor.actorSO.controllerSO.GetMoveSpeed(moveState);

        MovementRequest request=new()
        {
            Source=moveState.ToString(),
            WorldPositionDelta=
                actor.simulation.locomotionData.DesiredWorldMoveDirection*
                speed*inputAmount*TickTime.deltaTime,
            ForwardPositionDelta=0f,
            YawDelta=0f,
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
        return state is LocomotionStateType.Run or LocomotionStateType.Sprint
            ?LocomotionStateType.Run
            :LocomotionStateType.Walk;
    }

    private void SetMoveParameter(bool immediate,float deltaTime)
    {
        Vector3 localDirection=actor.player.InverseTransformDirection(
            actor.simulation.locomotionData.DesiredWorldMoveDirection);
        Vector2 targetParameter=new(localDirection.x,localDirection.z);
        presentedParameter=immediate
            ?targetParameter
            :Vector2.MoveTowards(
                presentedParameter,
                targetParameter,
                actor.actorSO.animationSO.Walk_Loop_SmoothFactor*
                deltaTime);
    }

    private void RefreshMoveAudio()
    {
        actor.audioSystem.StopLoop();
        actor.audioSystem.PlayLoop(
            presentedState==LocomotionStateType.Walk?"Walk":"Jog");
    }

}
