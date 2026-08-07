using System;

public class StateMachine
{
    public BaseState CurrentState;
    private Action onEndCallback;
    private Func<BaseState,BaseState>globalTransitionSelector;

    public void Initialize(BaseState startState)
    {
        CurrentState=startState;
        ClearOnEndCallback();
        CurrentState.Enter();
    }

    public void ServerTick()
    {
        BaseState stateBeforeTransition=CurrentState;
        BaseState globalTarget=globalTransitionSelector?.Invoke(stateBeforeTransition);
        //不相同需要切换状态
        if(globalTarget!=null&&!ReferenceEquals(globalTarget,stateBeforeTransition))
            ChangeState(globalTarget);
        else
            stateBeforeTransition.ServerTick();

        CurrentState.EvaluateMotion();
        CheckEnd();
    }

    public void SetGlobalTransitionSelector(Func<BaseState,BaseState>selector)
    {
        globalTransitionSelector=selector;
    }

    public void PresentationUpdate(float deltaTime)
    {
        CurrentState.PresentationUpdate(deltaTime);
        CurrentState.ApplyParameter();
    }

    public void SetOnEndCallback(Action callback)
    {
        onEndCallback=callback;
    }

    public void ChangeState(BaseState newState)
    {
        //切换状态自动清理结束回调
        CurrentState.Exit();
        ClearOnEndCallback();
        CurrentState=newState;
        CurrentState.Enter();
    }

    private void CheckEnd()
    {
        if(onEndCallback==null||CurrentState.NormalizedTime<1f)return;

        Action callback=onEndCallback;
        ClearOnEndCallback();
        callback.Invoke();
    }

    private void ClearOnEndCallback()
    {
        onEndCallback=null;
    }
}
