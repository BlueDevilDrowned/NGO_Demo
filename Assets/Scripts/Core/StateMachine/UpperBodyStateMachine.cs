using System;

public class UpperBodyStateMachine
{
    public UpperBodyState CurrentState{get;private set;}

    private Action onEndCallback;

    public void Initialize(UpperBodyState startState)
    {
        if(startState==null)
            throw new ArgumentNullException(nameof(startState));

        CurrentState=startState;
        ClearOnEndCallback();
        CurrentState.Enter();
    }

    public void ServerTick()
    {
        CurrentState?.ServerTick();
        CheckEnd();
    }

    public void PresentationUpdate(float deltaTime)
    {
        CurrentState?.PresentationUpdate(deltaTime);
        CurrentState?.ApplyParameter();
    }

    public void ChangeState(UpperBodyState next)
    {
        if(next==null||ReferenceEquals(CurrentState,next))return;
        CurrentState?.Exit();
        ClearOnEndCallback();
        CurrentState=next;
        CurrentState.Enter();
    }

    public void SetOnEndCallback(Action callback)
    {
        onEndCallback=callback;
    }

    public void ApplyAuthoritativeState(UpperBodyState state)
    {
        ChangeState(state);
    }

    private void CheckEnd()
    {
        if(onEndCallback==null||CurrentState==null||
           CurrentState.NormalizedTime<1f)return;

        Action callback=onEndCallback;
        ClearOnEndCallback();
        callback.Invoke();
    }

    private void ClearOnEndCallback()
    {
        onEndCallback=null;
    }
}
