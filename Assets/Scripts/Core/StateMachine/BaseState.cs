public class BaseState
{
    public virtual float NormalizedTime=>0f;
    public virtual void Enter(){}
    public virtual void ServerTick(){}//服务器更新，仅主机
    public virtual void PresentationUpdate(float deltaTime){}//渲染层更新
    public virtual void ApplyParameter(){}
    public virtual void EvaluateMotion(){}
    public virtual void Exit(){}
}
