public class BaseState
{
    public virtual float NormalizedTime=>0f;
    // 只判断能否进入，不在这里消费输入或产生其他副作用。
    public virtual bool CanEnterFrom(BaseState currentState)=>false;
    public virtual void Enter(){}
    public virtual void ServerTick(){}//服务器更新，仅主机
    public virtual void PresentationUpdate(float deltaTime){}//渲染层更新
    public virtual void ApplyParameter(){}
    public virtual void EvaluateMotion(){}
    public virtual void Exit(){}
}
