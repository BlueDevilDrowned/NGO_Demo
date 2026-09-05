using System;

/**
 * 状态机类，用于管理游戏中的状态转换和更新
 * 包含当前状态以及状态转换相关的处理逻辑
 */
public class StateMachine
{
    // 当前状态属性，仅可读
    public BaseState CurrentState{get;private set;}
    // 状态结束时的回调函数
    private Action onEndCallback;
    // 全局状态转换选择器，用于决定是否需要转换状态
    private Func<BaseState,BaseState>globalTransitionSelector;

    /**
     * 初始化状态机
     * @param startState 初始状态
     * @throws ArgumentNullException 当startState为null时抛出
     */
    public void Initialize(BaseState startState)
    {
        if(startState==null)throw new ArgumentNullException(nameof(startState));

        CurrentState=startState;
        ClearOnEndCallback();
        CurrentState.Enter();
    }

    /**
     * 服务器端状态更新，处理状态转换和状态更新
     * 在服务器帧更新时调用
     */
    public void ServerTick()
    {
        BaseState stateBeforeTransition=CurrentState;
        // 检查是否需要全局状态转换
        BaseState globalTarget=globalTransitionSelector?.Invoke(stateBeforeTransition);
        if(globalTarget!=null&&!ReferenceEquals(globalTarget,stateBeforeTransition))
            ChangeState(globalTarget);
        else
            stateBeforeTransition.ServerTick();

        // 评估动作状态并检查是否结束
        CurrentState.EvaluateMotion();
        CheckEnd();
    }

    /**
     * 设置全局状态转换选择器
     * @param selector 状态转换选择函数
     */
    public void SetGlobalTransitionSelector(Func<BaseState,BaseState>selector)
    {
        globalTransitionSelector=selector;
    }

    /**
     * 表现层更新，用于更新视觉效果和参数
     * @param deltaTime 自上一帧以来的时间差
     */
    public void PresentationUpdate(float deltaTime)
    {
        if(CurrentState==null)return;

        CurrentState.PresentationUpdate(deltaTime);
        CurrentState.ApplyParameter();
    }

    public void CheckPresentationEnd()
    {
        CheckEnd();
    }

    public void ReenterCurrentState()
    {
        if(CurrentState==null)return;

        CurrentState.Exit();
        ClearOnEndCallback();
        CurrentState.Enter();
    }

    public void Stop()
    {
        CurrentState?.Exit();
        ClearOnEndCallback();
        CurrentState=null;
    }

    /**
     * 设置状态结束时的回调函数
     * @param callback 回调函数
     */
    public void SetOnEndCallback(Action callback)
    {
        onEndCallback=callback;
    }

    /**
     * 改变状态
     * @param newState 新状态
     */
    public void ChangeState(BaseState newState)
    {
        if(newState==null)return;
        ChangeStateInternal(newState);
    }

    /**
     * 检查状态是否结束
     * 如果状态结束时间达到1且设置了回调函数，则触发回调
     */
    private void CheckEnd()
    {
        if(onEndCallback==null||CurrentState.NormalizedTime<1f)return;

        Action callback=onEndCallback;
        ClearOnEndCallback();
        callback.Invoke();
    }

    // 清除状态结束回调
    private void ClearOnEndCallback()
    {
        onEndCallback=null;
    }

    /**
     * 内部状态转换实现
     * @param newState 新状态
     */
    private void ChangeStateInternal(BaseState newState)
    {
        if(ReferenceEquals(CurrentState,newState))return;

        // 退出当前状态，清除回调，设置并进入新状态
        CurrentState?.Exit();
        ClearOnEndCallback();
        CurrentState=newState;
        CurrentState.Enter();
    }
}
