public class ActorLandState : ActorBaseState
{
    public ActorLandState(Actor actor) : base(actor)
    {
    }
    public override void Enter()
    {
        bool hasMoveIntent=
            actor.simulation.locomotionData.stateType!=LocomotionStateType.Idle;

        AirborneFullBodyAnimations airborne=Animations?.Airborne;
        Animancer.TransitionAsset landing=hasMoveIntent
            ?airborne?.LandToMove??airborne?.Land
            :airborne?.Land;

        Play(landing);
        if(landing!=null)
            stateMachine.SetOnEndCallback(OnLandingEnd);
        else
            OnLandingEnd();

        //
        actor.audioSystem.PlayOneShot("Land");
    }
    public override void ServerTick()
    {
        if(NormalizedTime<0.6f)return;

        if(actor.simulation.WantMove)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorMoveLoopState>());
            return;
        }
    }

    private void OnLandingEnd()
    {
        if(stateMachine.CurrentState!=this)return;

        if(actor.simulation.locomotionData.stateType==LocomotionStateType.Idle)
        {
            stateMachine.ChangeState(stateRegistry.GetState<ActorIdleState>());
            return;
        }

        stateMachine.ChangeState(stateRegistry.GetState<ActorMoveLoopState>());
    }

}
