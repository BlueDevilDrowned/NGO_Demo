public class BaseState
{
    public virtual float NormalizedTime=>0f;
    public virtual void Enter(){}
    public virtual void ServerTick(){}
    public virtual void EvaluateMotion(){}
    public virtual void Exit(){}

}
