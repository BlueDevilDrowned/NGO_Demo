using System;

public class StateMachine
{
    public BaseState CurrentState;
    private Action onEndCallback;

    public void Initialize(BaseState startState)
    {
        CurrentState=startState;
        ClearOnEndCallback();
        CurrentState.Enter();
    }

    public void ServerTick()
    {
        CurrentState.ServerTick();
        CurrentState.EvaluateMotion();
        CheckEnd();
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
