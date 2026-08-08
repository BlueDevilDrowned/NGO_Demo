public class BaseState
{
    public AimModePolicy AimModePolicy{get;private set;}=AimModePolicy.ForceNormal;
    public virtual float NormalizedTime=>0f;

    public virtual bool CanEnterFrom(BaseState currentState)=>false;
    public virtual void Enter(){}
    public virtual void ServerTick(){}
    public virtual void PresentationUpdate(float deltaTime){}
    public virtual void ApplyParameter(){}
    public virtual void EvaluateMotion(){}
    public virtual void Exit(){}

    internal void BindConfig(ActorStateConfig config)
    {
        if(config==null)throw new System.ArgumentNullException(nameof(config));
        AimModePolicy=config.AimModePolicy;
    }
}
