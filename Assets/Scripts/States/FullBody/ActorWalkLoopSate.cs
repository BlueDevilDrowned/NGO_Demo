using UnityEngine;

public class ActorWalkLoopState : ActorBaseState
{
    private const float LeanEnterThreshold=0.05f;

    private bool StartFootIsL;
    private bool IsLeaning;

    public ActorWalkLoopState(Actor actor) : base(actor)
    {
    }

    public override void Enter()
    {
        IsLeaning=false;

        if(actor.runTimeData.blackboard.StartFootIsL)
        {
            animation.PlayTransition(actor.animancerData.Walk_Loop_L,AnimPlayOptions.Default);
            StartFootIsL=true;
        }
        else
        {
            animation.PlayTransition(actor.animancerData.Walk_Loop_R,AnimPlayOptions.Default);
            StartFootIsL=false;
        }
    }

    public override void Exit()
    {
        IsLeaning=false;
    }

    public override void ServerTick()
    {
        if(!actor.runTimeData.WantMove)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorWalkStopState>());
            return;
        }

        float maxYawDelta=actor.controllerSO.WalkmaxRotation*TickTime.deltaTime;
        float yawDelta=Mathf.Clamp(
            actor.runTimeData.DesiredLocalMoveAngle,
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

        animation.PlayTransition(actor.animancerData.Walk_Loop_Lean,options);
    }

    public override void ApplyParameter()
    {
        animation.SetMixerParameter(actor.runTimeData.blackboard.Parameter);
    }

    public override void EvaluateMotion()
    {
        float maxYawDelta=actor.controllerSO.WalkmaxRotation*TickTime.deltaTime;
        float yawDelta=Mathf.Clamp(
            actor.runTimeData.DesiredLocalMoveAngle,
            -maxYawDelta,
            maxYawDelta);
        float inputAmount=Mathf.Clamp01(actor.runTimeData.Input.InputMove.magnitude);

        MovementRequest request=new()
        {
            Source="Walk",
            WorldPositionDelta=Vector3.zero,
            ForwardPositionDelta=actor.controllerSO.WalkSpeed*inputAmount*TickTime.deltaTime,
            YawDelta=yawDelta,
        };

        actor.movement.Submit(request);
    }
}
