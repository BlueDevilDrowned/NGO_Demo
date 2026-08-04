public class BaseState
{
    public virtual float NormalizedTime=>0f;
    public virtual void Enter(){}
    public virtual void ServerTick(){}
    public virtual void ApplyParameter(){}//用于每帧往mixer里写值
    public virtual void EvaluateMotion(){}
    public virtual void Exit(){}

}
