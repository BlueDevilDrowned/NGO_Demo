using System;

public class StateMachine
{
    public BaseState CurrentState;
    public ActorMode CurrentMode{get;private set;}=ActorMode.Normal;

    private Action onEndCallback;
    private Func<BaseState,BaseState>globalTransitionSelector;
    private Action<ActorMode>stateModeChanged;

    public void Initialize(BaseState startState)
    {
        if(startState==null)throw new ArgumentNullException(nameof(startState));

        CurrentState=startState;
        ClearOnEndCallback();
        ApplyMode(ResolveMode(startState));
        CurrentState.Enter();
    }

    public void ServerTick()
    {
        BaseState stateBeforeTransition=CurrentState;
        BaseState globalTarget=globalTransitionSelector?.Invoke(stateBeforeTransition);
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

    public void SetStateModeChangedHandler(Action<ActorMode>handler)
    {
        stateModeChanged=handler;
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
        if(newState==null)return;
        ChangeStateInternal(newState,ResolveMode(newState));
    }

    public void ApplyAuthoritativeState(
        BaseState newState,
        ActorMode authoritativeMode)
    {
        if(newState==null)return;
        //同步状态和模式
        if(ReferenceEquals(CurrentState,newState))
        {
            ApplyMode(authoritativeMode);
            return;
        }

        ChangeStateInternal(newState,authoritativeMode);
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

    private void ChangeStateInternal(BaseState newState,ActorMode newMode)
    {
        if(ReferenceEquals(CurrentState,newState))return;

        CurrentState?.Exit();
        ClearOnEndCallback();
        CurrentState=newState;
        ApplyMode(newMode);
        CurrentState.Enter();
    }

    private ActorMode ResolveMode(BaseState targetState)
    {
        return targetState.AimModePolicy switch
        {
            AimModePolicy.ForceNormal=>ActorMode.Normal,
            AimModePolicy.ForceAiming=>ActorMode.Aiming,
            AimModePolicy.Preserve=>CurrentMode,
            _=>CurrentMode,
        };
    }

    private void ApplyMode(ActorMode newMode)
    {
        //模式变化的时候切换
        ActorMode oldMode=CurrentMode;
        if(oldMode==newMode)return;

        CurrentMode=newMode;
        stateModeChanged?.Invoke(newMode);
    }
}
