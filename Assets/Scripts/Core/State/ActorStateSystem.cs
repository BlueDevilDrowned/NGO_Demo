using System;

public sealed class ActorStateSystem : IActorSystem
{
    private readonly Actor actor;
    private readonly ActorStateReplication replication;
    private bool isInitialized;
    private bool hasCapturedState;
    private ActorStateType capturedStateType;
    private uint stateEnterTick;

    public StateMachine Machine{get;}
    public ActorStateRegistry Registry{get;}

    public ActorStateSystem(Actor actor)
    {
        this.actor=actor??throw new ArgumentNullException(nameof(actor));
        Machine=new StateMachine();
        Registry=new ActorStateRegistry();
        replication=new ActorStateReplication(actor);
        actor.RegisterSystem(this);
    }

    public void Initialize(ActorBrainSo brain)
    {
        if(isInitialized)return;
        if(brain==null)throw new ArgumentNullException(nameof(brain));

        Registry.Initialize(brain,actor);

        ActorGlobalTransitionResolver transitions=
            new ActorGlobalTransitionResolver(brain,Registry);
        Machine.SetGlobalTransitionSelector(transitions.SelectNextState);
        Machine.Initialize(Registry.InitialState);

        isInitialized=true;
        CaptureAuthoritativeState(0);
    }

    public void ServerTick(uint tick)
    {
        if(!actor.IsServer)return;

        Machine.ServerTick();
        CaptureAuthoritativeState(tick);
    }

    public void PresentationUpdate(float deltaTime)
    {
        if(replication.TryConsumeState(out ActorStateSnapshot snapshot)&&
           Registry.TryGetState(snapshot.StateType,out ActorBaseState state))
        {
            Machine.ChangeState(state);
        }

        Machine.PresentationUpdate(deltaTime);
    }

    private void CaptureAuthoritativeState(uint tick)
    {
        if(!actor.IsServer||
           Machine.CurrentState is not ActorBaseState currentState||
           !Registry.TryGetStateType(currentState,out ActorStateType stateType))
            return;

        if(hasCapturedState&&stateType==capturedStateType)return;

        hasCapturedState=true;
        capturedStateType=stateType;
        stateEnterTick=tick;
        replication.MarkAuthoritativeState(new ActorStateSnapshot
        {
            StateType=stateType,
            StateEnterTick=stateEnterTick,
        });
    }

    public void Dispose()
    {
        replication.Dispose();
    }
}
